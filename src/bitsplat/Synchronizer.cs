using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;
using PeanutButter.Utils;

namespace bitsplat
{
    public interface ISynchronizer
    {
        SyncResult Synchronize(
            IFileSystem from,
            IFileSystem to
        );

        SyncResult Synchronize(
            string label,
            IFileSystem from,
            IFileSystem to
        );
    }

    public class Synchronizer
        : ISynchronizer
    {
        private readonly ITargetHistoryRepository _targetHistoryRepository;
        private readonly IResumeStrategy _resumeStrategy;
        private readonly IPassThrough[] _intermediatePipes;
        private readonly IFilter[] _filters;
        private readonly IProgressReporter _progressReporter;
        private readonly IOptions _options;
        private readonly ISyncQueueNotifiable[] _notifiables;
        private string _label;

        public Synchronizer(
            ITargetHistoryRepository targetHistoryRepository,
            IResumeStrategy resumeStrategy,
            IPassThrough[] intermediatePipes,
            IFilter[] filters,
            IProgressReporter progressReporter,
            IOptions options)
        {
            ValidateIntermediatePipes(intermediatePipes);
            _targetHistoryRepository = targetHistoryRepository;
            _resumeStrategy = resumeStrategy;
            _intermediatePipes = intermediatePipes;
            _notifiables = intermediatePipes
                .OfType<ISyncQueueNotifiable>()
                .ToArray();
            _filters = filters;
            _progressReporter = progressReporter;
            _options = options;
        }

        private void ValidateIntermediatePipes(IPassThrough[] intermediatePipes)
        {
            intermediatePipes.ForEach(p =>
            {
                if (!(p is PassThrough))
                {
                    throw new InvalidOperationException(
                        "PassThrough pipes _must_ inherit from the abstract PassThrough class to ensure correct pipeline disposal on error"
                    );
                }
            });
        }

        public SyncResult Synchronize(
            string label,
            IFileSystem from,
            IFileSystem to)
        {
            _label = label;
            return Synchronize(from, to);
        }

        public SyncResult Synchronize(
            IFileSystem source,
            IFileSystem target
        )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResourcesCollection = target.ListResourcesRecursive();
            var targetResources = targetResourcesCollection as IReadWriteFileResource[] ??
                                  targetResourcesCollection.ToArray();

            var comparison = CompareResources(
                sourceResources,
                targetResources,
                source,
                target);
            RecordSkipped(comparison);

            var syncQueue = comparison.SyncQueue
                .OrderBy(resource => resource.RelativePath)
                .ToArray();

            NotifySyncBatchStart(syncQueue);
            if (syncQueue.IsEmpty())
            {
                NotifyNoWork(source, target);
                return SyncResult.NothingAttempted;
            }

            if (_options.DryRun)
            {
                Debug("would attempt to sync:");
                syncQueue.ForEach(LogWouldAttemptSync);
                return SyncResult.NothingAttempted;
            }

            var synced = new List<IReadWriteFileResource>();
            var failed = new List<IReadWriteFileResource>();
            syncQueue.ForEach(sourceResource =>
            {
                var succeeded = SynchroniseResource(
                    source,
                    sourceResource,
                    target,
                    targetResources
                );
                (succeeded ? synced : failed).Add(sourceResource);
            });

            NotifySyncBatchComplete(syncQueue);
            return new SyncResult(synced, failed);
        }

        private void LogWouldAttemptSync(IReadWriteFileResource obj)
        {
            Debug($"  {obj.RelativePath} ({obj.Size})");
        }

        private void Debug(string message)
        {
            if (_options.Verbose)
            {
                _notifiables.ForEach(n => n.Log(message));
            }
        }

        private bool SynchroniseResource(
            IFileSystem source,
            IReadWriteFileResource sourceResource,
            IFileSystem target,
            IReadWriteFileResource[] targetResources)
        {
            if (!source.IsFile(sourceResource.RelativePath))
            {
                NotifyError(
                    sourceResource,
                    null,
                    new FileNotFoundException(
                        $"source: {sourceResource.RelativePath}"
                    )
                );
                return false;
            }

            var maxAttempts = 1 + _options.Retries;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (TrySynchroniseResource(
                    sourceResource,
                    target,
                    targetResources
                ))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryOpen(
            Func<Stream> open,
            IFileResource sourceResource,
            IFileResource targetResource,
            out Stream stream)
        {
            try
            {
                stream = open();
                return true;
            }
            catch (Exception ex)
            {
                NotifyError(sourceResource, targetResource, ex);
                stream = null;
                return false;
            }
        }

        private bool TrySynchroniseResource(
            IReadWriteFileResource sourceResource,
            IFileSystem target,
            IReadWriteFileResource[] targetResources)
        {
            var targetResource = targetResources.FirstOrDefault(
                r => r.RelativePath == sourceResource.RelativePath
            );

            if (!TryOpen(
                    sourceResource.OpenForRead,
                    sourceResource,
                    targetResource,
                    out var sourceStream))
            {
                return false;
            }

            Stream targetStream = null;
            try
            {
                if (!TryOpen(
                        () => target.Open(
                            sourceResource.RelativePath,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite
                        ),
                        sourceResource,
                        targetResource,
                        out targetStream))
                {
                    return false;
                }
            }
            finally
            {
                if (targetStream is null)
                {
                    sourceStream.Dispose();
                }
            }

            return TrySynchroniseStreams(
                sourceResource,
                target,
                targetResource,
                sourceStream,
                targetStream);
        }

        private bool TrySynchroniseStreams(
            IReadWriteFileResource sourceResource,
            IFileSystem target,
            IReadWriteFileResource targetResource,
            Stream sourceStream,
            Stream targetStream)
        {
            NotifySyncStart(sourceResource, targetResource, sourceStream, targetStream);

            var composition = ComposePipeline(sourceStream, targetStream);
            try
            {
                composition.Drain();
                NotifySyncComplete(
                    sourceResource,
                    targetResource ??
                    CreateFileResourcePropertiesFor(
                        target,
                        sourceResource)
                );
                RecordHistory(sourceResource);
            }
            catch (Exception ex)
            {
                NotifyError(sourceResource, targetResource, ex);
                composition.Dispose();
                return false;
            }

            return true;
        }

        private void NotifySyncStart(IReadWriteFileResource sourceResource,
            IReadWriteFileResource targetResource,
            Stream sourceStream,
            Stream targetStream)
        {
            var isResuming = ResumeIfPossible(
                sourceResource,
                targetResource,
                sourceStream,
                targetStream);

            NotifySyncStart(
                sourceResource,
                isResuming
                    ? targetResource
                    : null);
        }

        private void RecordSkipped(FileSystemComparison comparison)
        {
            if (_options.NoHistory)
            {
                // fixme: this is a mix of concerns:
                // there's already a noop history reporter
                // -> just trying to reduce logging
                return;
            }

            var toRecord = comparison.Skipped
                .Union(comparison.RecordOnly)
                .ToArray();
            if (toRecord.Any())
            {
                _progressReporter.Bookend(
                    "Recording skipped item history",
                    () =>
                    {
                        RecordHistory(
                            toRecord
                        );
                    }
                );
            }
        }

        private class FileSystemComparison
        {
            public List<IReadWriteFileResource> SyncQueue { get; } = new List<IReadWriteFileResource>();
            public List<IReadWriteFileResource> Skipped { get; } = new List<IReadWriteFileResource>();
            public List<IReadWriteFileResource> Excluded { get; } = new List<IReadWriteFileResource>();
            public List<IReadWriteFileResource> RecordOnly { get; } = new List<IReadWriteFileResource>();
        }

        private class FileResource
            : BasicFileResource,
              IFileResource
        {
            public override string Path { get; }
            public override long Size { get; }
            public override string RelativePath { get; }

            public FileResource(
                IFileSystem targetFileSystem,
                IReadWriteFileResource sourceResource)
            {
                Path = System.IO.Path.Combine(
                    targetFileSystem.BasePath,
                    sourceResource.RelativePath);
                RelativePath = sourceResource.RelativePath;
                Size = sourceResource.Size;
            }
        }

        private IFileResource CreateFileResourcePropertiesFor(
            IFileSystem target,
            IReadWriteFileResource sourceResource)
        {
            return new FileResource(
                target,
                sourceResource);
        }

        private void NotifyNoWork(
            IFileSystem source,
            IFileSystem target)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifyNoWork(
                    source,
                    target
                )
            );
        }

        private void NotifyError(
            IFileResource source,
            IFileResource target,
            Exception ex)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifyError(
                    source,
                    target,
                    ex
                )
            );
        }

        private void NotifySyncBatchStart(IEnumerable<IReadWriteFileResource> resources)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchStart(
                    _label,
                    resources
                )
            );
        }

        private void NotifySyncBatchComplete(IReadWriteFileResource[] syncQueue)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchComplete(
                    _label,
                    syncQueue
                )
            );
        }

        private void NotifySyncStart(
            IReadWriteFileResource source,
            IReadWriteFileResource target)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncStart(
                    source,
                    target)
            );
        }

        private void NotifySyncComplete(
            IFileResource sourceResource,
            IFileResource targetResource
        )
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncComplete(
                    sourceResource,
                    targetResource)
            );
        }

        private bool ResumeIfPossible(
            IReadWriteFileResource sourceResource,
            IReadWriteFileResource targetResource,
            Stream sourceStream,
            Stream targetStream)
        {
            var canResume = targetResource != null &&
                            _resumeStrategy.CanResume(
                                sourceResource,
                                targetResource,
                                sourceStream,
                                targetStream);
            if (canResume)
            {
                sourceStream.Seek(targetResource.Size, SeekOrigin.Begin);
                targetStream.Seek(targetResource.Size, SeekOrigin.Begin);
            }
            else
            {
                // force restart
                targetStream.SetLength(0);
            }

            return canResume;
        }

        private FileSystemComparison CompareResources(
            IEnumerable<IReadWriteFileResource> sourceResources,
            IEnumerable<IReadWriteFileResource> targetResources,
            IFileSystem source,
            IFileSystem target)
        {
            var result = _progressReporter.Bookend(
                "Comparing source and target",
                () => sourceResources.Aggregate(
                    new FileSystemComparison(),
                    (acc, sourceResource) =>
                    {
                        var filterResult = ApplyAllFilters(
                            targetResources,
                            sourceResource,
                            source,
                            target
                        );

                        List<IReadWriteFileResource> list = null;
                        switch (filterResult)
                        {
                            case FilterResult.Include:
                                list = acc.SyncQueue;
                                break;
                            case FilterResult.Ambivalent:
                                list = acc.Skipped;
                                break;
                            case FilterResult.Exclude:
                                list = acc.Excluded;
                                break;
                            case FilterResult.RecordOnly:
                            default:
                                list = acc.RecordOnly;
                                break;
                        }
                        list.Add(sourceResource);
                        return acc;
                    })
            );
            Log(result);
            return result;
        }

        private void Log(FileSystemComparison result)
        {
            if (!_options.Verbose)
            {
                return;
            }
            Dump("Excluded", result.Excluded);
            Dump("Skipped", result.Skipped);
            Dump("Record only", result.RecordOnly);
            Dump("Sync", result.SyncQueue);
        }

        private void Dump(
            string label,
            List<IReadWriteFileResource> resources)
        {
            if (resources.None())
            {
                return;
            }

            Debug($"{label}:");
            resources.ForEach(r => Debug($"  {r.RelativePath}"));
        }

        private FilterResult ApplyAllFilters(
            IEnumerable<IReadWriteFileResource> targetResources,
            IReadWriteFileResource sourceResource,
            IFileSystem source,
            IFileSystem target)
        {
            var filterResult = _filters.Aggregate(
                FilterResult.Ambivalent,
                (acc1, cur1) =>
                {
                    if (AlreadyExcluded() ||
                        // "record only" is a soft exclude
                        // to ensure history happens for existing files
                        AccumulatorIsRecordOnly())
                    {
                        return acc1;
                    }

                    var thisResult = cur1.Filter(
                        sourceResource,
                        targetResources,
                        _targetHistoryRepository,
                        source,
                        target);

                    return CurrentFilterIsAmbivalent()
                               ? acc1
                               : thisResult;

                    bool AlreadyExcluded()
                    {
                        return acc1 == FilterResult.Exclude;
                    }

                    bool CurrentFilterIsAmbivalent()
                    {
                        return thisResult == FilterResult.Ambivalent;
                    }

                    bool AccumulatorIsRecordOnly()
                    {
                        return acc1 == FilterResult.RecordOnly;
                    }
                });
            return filterResult;
        }

        private void RecordHistory(IReadWriteFileResource readWriteFileResource)
        {
            _targetHistoryRepository.Upsert(
                new HistoryItem(readWriteFileResource)
            );
        }

        private void RecordHistory(
            IEnumerable<IReadWriteFileResource> resources
        )
        {
            _targetHistoryRepository.Upsert(
                resources
                    .Select(r => new HistoryItem(r))
                    .ToArray()
            );
        }

        private ISink ComposePipeline(
            Stream source,
            Stream target)
        {
            if (!_intermediatePipes.Any())
            {
                return source.Pipe(target);
            }

            _intermediatePipes.ForEach(intermediate => intermediate.Detach());
            var composition = _intermediatePipes.Aggregate(
                source.Pipe(new NullPassThrough()),
                (acc, cur) => acc.Pipe(cur)
            );
            return composition.Pipe(target);
        }

        private class HistoryItem : IHistoryItem
        {
            public int Id { get; set; }
            public string Path { get; set; }
            public long Size { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Modified { get; set; }

            public HistoryItem(IReadWriteFileResource resource)
            {
                Path = resource.RelativePath;
                Size = resource.Size;
            }
        }
    }
}