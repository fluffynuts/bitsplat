using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly ISyncQueueNotifiable[] _notifiables;

        public Synchronizer(
            ITargetHistoryRepository targetHistoryRepository,
            IResumeStrategy resumeStrategy,
            IPassThrough[] intermediatePipes,
            IResourceMatcher[] resourceMatchers)
        {
            _targetHistoryRepository = targetHistoryRepository;
            _resumeStrategy = resumeStrategy;
            _intermediatePipes = intermediatePipes;
            _notifiables = intermediatePipes
                .OfType<ISyncQueueNotifiable>()
                .ToArray();
            _resourceMatchers = resourceMatchers;
        }

        private class FileSystemComparison
        {
            public List<IFileResource> SyncQueue { get; } = new List<IFileResource>();
            public List<IFileResource> Skipped { get; } = new List<IFileResource>();
        }

        private class FileResourceProperties : IFileResourceProperties
        {
            public string Path { get; }
            public long Size { get; }
            public string RelativePath { get; }

            public FileResourceProperties(
                IFileSystem targetFileSystem,
                IFileResource sourceResource)
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
            var targetResources = targetResourcesCollection as IFileResource[] ?? targetResourcesCollection.ToArray();
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

        private IFileResourceProperties CreateFileResourcePropertiesFor(
            IFileSystem target,
            IFileResource sourceResource)
        {
            return new FileResourceProperties(
                target,
                sourceResource);
        }

        private void NotifySyncBatchComplete(IFileResource[] syncQueue)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchComplete(syncQueue)
            );
        }

        private void NotifySyncComplete(
            IFileResourceProperties sourceResource,
            IFileResourceProperties targetResource)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncComplete(
                    sourceResource,
                    targetResource)
            );
        }

        private void NotifySyncStart(
            IFileResource source,
            IFileResource target)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncStart(
                    source,
                    target)
            );
        }

        private void NotifySyncBatchStart(IEnumerable<IFileResource> resources)
        {
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatchStart(resources)
            );
        }

        private bool ResumeIfPossible(IFileResource targetResource,
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
            IEnumerable<IFileResource> sourceResources,
            IEnumerable<IFileResource> targetResources)
        {
            var comparison = sourceResources.Aggregate(
                new FileSystemComparison(),
                (acc, sourceResource) =>
                {
                    var list = targetResources.Any(
                                   targetResource => _resourceMatchers.Aggregate(
                                       true,
                                       (matched, cur) => matched && cur.AreMatched(sourceResource, targetResource)
                                   ))
                                   ? acc.Skipped
                                   : acc.SyncQueue;
                    list.Add(sourceResource);
                    return acc;
                });
            return comparison;
        }

        private void RecordHistory(IFileResource fileResource)
        {
            _targetHistoryRepository.Upsert(
                new HistoryItem(fileResource)
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

            public HistoryItem(IFileResource resource)
            {
                Path = resource.RelativePath;
                Size = resource.Size;
            }
        }
    }
}