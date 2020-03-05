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
        void Synchronize(
            IFileSystem from,
            IFileSystem to
        );

        void Synchronize(
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

        public void Synchronize(
            string label,
            IFileSystem from,
            IFileSystem to)
        {
            _label = label;
            Synchronize(from, to);
        }

        public void Synchronize(
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
                return;
            }

            syncQueue.ForEach(sourceResource =>
                SynchroniseResource(
                    source,
                    sourceResource,
                    target,
                    targetResources
                )
            );

            NotifySyncBatchComplete(syncQueue);
        }

        private void SynchroniseResource(
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
                return;
            }

            var attempts = 0;
            var test = _options.Retries < 1
                           ? new Func<bool>(() => true)
                           : CanTryAgain;
            do
            {
                if (TrySynchroniseResource(
                    sourceResource,
                    target,
                    targetResources
                ))
                {
                    return;
                }
            } while (test());

            bool CanTryAgain()
            {
                return ++attempts < _options.Retries;
            }
        }

        private bool TrySynchroniseResource(
            IReadWriteFileResource sourceResource,
            IFileSystem target,
            IReadWriteFileResource[] targetResources)
        {
            var targetResource = targetResources.FirstOrDefault(
                r => r.RelativePath == sourceResource.RelativePath);

            // FIXME: if the source or target can't be opened
            // then the entire process bombs
            var sourceStream = sourceResource.OpenForRead();
            var targetStream = target.Open(
                sourceResource.RelativePath,
                FileMode.OpenOrCreate);

            var resuming = ResumeIfPossible(
                sourceResource,
                targetResource,
                sourceStream,
                targetStream);

            NotifySyncStart(
                sourceResource,
                resuming
                    ? targetResource
                    : null);

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

        private void RecordSkipped(FileSystemComparison comparison)
        {
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

            return canResume;
        }

        private FileSystemComparison CompareResources(
            IEnumerable<IReadWriteFileResource> sourceResources,
            IEnumerable<IReadWriteFileResource> targetResources,
            IFileSystem source,
            IFileSystem target)
        {
            return _progressReporter.Bookend(
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