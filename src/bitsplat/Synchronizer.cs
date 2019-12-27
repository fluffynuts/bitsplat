using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResourceMatchers;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;

namespace bitsplat
{
    public interface ISynchronizer
    {
        void Synchronize(
            IFileSystem from,
            IFileSystem to);
    }

    public class Synchronizer
        : ISynchronizer
    {
        private readonly ITargetHistoryRepository _targetHistoryRepository;
        private readonly IResumeStrategy _resumeStrategy;
        private readonly IPassThrough[] _intermediatePipes;
        private readonly IResourceMatcher[] _resourceMatchers;
        private readonly IFilter[] _filters;
        private readonly ISyncQueueNotifiable[] _notifiables;

        public Synchronizer(
            ITargetHistoryRepository targetHistoryRepository,
            IResumeStrategy resumeStrategy,
            IPassThrough[] intermediatePipes,
            IResourceMatcher[] resourceMatchers,
            IFilter[] filters)
        {
            _targetHistoryRepository = targetHistoryRepository;
            _resumeStrategy = resumeStrategy;
            _intermediatePipes = intermediatePipes;
            _notifiables = intermediatePipes
                .OfType<ISyncQueueNotifiable>()
                .ToArray();
            _resourceMatchers = resourceMatchers;
            _filters = filters;
        }

        private class FileSystemComparison
        {
            public List<IReadWriteFileResource> SyncQueue { get; } = new List<IReadWriteFileResource>();
            public List<IReadWriteFileResource> Skipped { get; } = new List<IReadWriteFileResource>();
        }

        private class FileResource : IFileResource
        {
            public string Path { get; }
            public long Size { get; }
            public string RelativePath { get; }

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

        public void Synchronize(
            IFileSystem source,
            IFileSystem target
        )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResourcesCollection = target.ListResourcesRecursive();
            var targetResources = targetResourcesCollection as IReadWriteFileResource[] ??
                                  targetResourcesCollection.ToArray();
            var comparison = CompareResources(sourceResources, targetResources);
            comparison.Skipped.ForEach(RecordHistory);

            var syncQueue = comparison.SyncQueue
                .OrderBy(resource => resource.RelativePath)
                .ToArray();

            NotifySyncBatchStart(syncQueue);

            syncQueue.ForEach(sourceResource =>
            {
                var targetResource = targetResources.FirstOrDefault(
                    r => r.RelativePath == sourceResource.RelativePath);

                var sourceStream = sourceResource.Read();
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
                composition.Drain();
                NotifySyncComplete(
                    sourceResource,
                    targetResource ??
                    CreateFileResourcePropertiesFor(
                        target,
                        sourceResource)
                );
                RecordHistory(sourceResource);
            });

            NotifySyncBatchComplete(syncQueue);
        }

        private IFileResource CreateFileResourcePropertiesFor(
            IFileSystem target,
            IReadWriteFileResource sourceResource)
        {
            return new FileResource(
                target,
                sourceResource);
        }

        private void NotifySyncBatchComplete(IReadWriteFileResource[] syncQueue)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchComplete(syncQueue)
            );
        }

        private void NotifySyncComplete(
            IFileResource sourceResource,
            IFileResource targetResource)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncComplete(
                    sourceResource,
                    targetResource)
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

        private void NotifySyncBatchStart(IEnumerable<IReadWriteFileResource> resources)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchStart(resources)
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
            var comparison = sourceResources.Aggregate(
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
                });
            return comparison;
        }

        private FilterResult ApplyAllFilters(IEnumerable<IReadWriteFileResource> targetResources,
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