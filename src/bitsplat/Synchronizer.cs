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
    public delegate string OnSynchronisationStart();

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
        private readonly ISyncQueueNotifiable[] _notifiables;
        private string _label;

        public Synchronizer(
            ITargetHistoryRepository targetHistoryRepository,
            IResumeStrategy resumeStrategy,
            IPassThrough[] intermediatePipes,
            IFilter[] filters,
            IProgressReporter progressReporter)
        {
            _targetHistoryRepository = targetHistoryRepository;
            _resumeStrategy = resumeStrategy;
            _intermediatePipes = intermediatePipes;
            _notifiables = intermediatePipes
                .OfType<ISyncQueueNotifiable>()
                .ToArray();
            _filters = filters;
            _progressReporter = progressReporter;
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
            NotifySyncPrepare(source, target);
            var sourceResources = source.ListResourcesRecursive();
            var targetResourcesCollection = target.ListResourcesRecursive();
            var targetResources = targetResourcesCollection as IReadWriteFileResource[] ??
                                  targetResourcesCollection.ToArray();

            var comparison = CompareResources(sourceResources, targetResources);
            _progressReporter.Bookend(
                "Recording skipped item history",
                () => RecordHistory(comparison.Skipped)
            );

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
            {
                var targetResource = targetResources.FirstOrDefault(
                    r => r.RelativePath == sourceResource.RelativePath);

                var sourceStream = sourceResource.OpenForRead();
                var targetStream = target.Open(
                    sourceResource.RelativePath,
                    FileMode.OpenOrCreate);

                var resuming = ResumeIfPossible(
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
                }
            });

            NotifySyncBatchComplete(syncQueue);
        }

        private class FileSystemComparison
        {
            public List<IReadWriteFileResource> SyncQueue { get; } = new List<IReadWriteFileResource>();
            public List<IReadWriteFileResource> Skipped { get; } = new List<IReadWriteFileResource>();
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

        private void NotifySyncPrepare(
            IFileSystem source,
            IFileSystem target)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchPrepare(
                    $"{_label} (prepare)",
                    source,
                    target
                )
            );
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

        private bool ResumeIfPossible(IReadWriteFileResource targetResource,
            Stream sourceStream,
            Stream targetStream)
        {
            var canResume = targetResource != null &&
                            _resumeStrategy.CanResume(
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
            IEnumerable<IReadWriteFileResource> targetResources)
        {
            return _progressReporter.Bookend(
                "Comparing source and target",
                () => sourceResources.Aggregate(
                    new FileSystemComparison(),
                    (acc, sourceResource) =>
                    {
                        var filterResult = ApplyAllFilters(
                            targetResources,
                            sourceResource
                        );

                        var list = filterResult == FilterResult.Include
                                       ? acc.SyncQueue
                                       : acc.Skipped;
                        list.Add(sourceResource);
                        return acc;
                    })
            );
        }

        private FilterResult ApplyAllFilters(
            IEnumerable<IReadWriteFileResource> targetResources,
            IReadWriteFileResource sourceResource)
        {
            var filterResult = _filters.Aggregate(
                FilterResult.Ambivalent,
                (acc1, cur1) =>
                {
                    if (AlreadyExcluded())
                    {
                        return acc1;
                    }

                    var thisResult = cur1.Filter(
                        sourceResource,
                        targetResources,
                        _targetHistoryRepository);

                    return CurrentFilterIsAmbivalent()
                               ? acc1
                               : thisResult; // otherwise return whatever this filter wants

                    bool AlreadyExcluded()
                    {
                        return acc1 == FilterResult.Exclude;
                    }

                    bool CurrentFilterIsAmbivalent()
                    {
                        return thisResult == FilterResult.Ambivalent;
                    }
                });
            return filterResult;
        }

        private bool ExistsInHistory(
            IReadWriteFileResource resource)
        {
            var historyItem = _targetHistoryRepository.Find(resource.RelativePath);
            return historyItem != null &&
                   historyItem.Size == resource.Size;
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
                resources.Select(r => new HistoryItem(r))
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
            public string Path { get; set; }
            public long Size { get; set; }

            public HistoryItem(IReadWriteFileResource resource)
            {
                Path = resource.RelativePath;
                Size = resource.Size;
            }
        }
    }
}