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

        public void Synchronize(
            IFileSystem source,
            IFileSystem target
        )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResourcesCollection = target.ListResourcesRecursive();
            var targetResources = targetResourcesCollection as IFileResource[] ?? targetResourcesCollection.ToArray();
            var syncQueue = sourceResources
                .Where(sourceResource =>
                    !targetResources.Any(
                        targetResource => _resourceMatchers.Aggregate(
                            true,
                            (acc, cur) => acc && cur.AreMatched(sourceResource, targetResource)
                        )
                    )
                )
                .OrderBy(resource => resource.RelativePath);
            _notifiables.ForEach(
                notifiable => notifiable.NotifySyncBatch(syncQueue)
            );
            syncQueue.ForEach(sourceResource =>
            {
                var sourceStream = sourceResource.Read();
                var targetStream = target.Open(sourceResource.RelativePath, FileMode.OpenOrCreate);

                var targetResource = targetResources.FirstOrDefault(
                    r => r.RelativePath == sourceResource.RelativePath);

                var canResume = targetResource != null &&
                                _resumeStrategy.CanResume(
                                    sourceStream,
                                    targetStream);
                if (canResume)
                {
                    sourceStream.Seek(targetResource.Size, SeekOrigin.Begin);
                    targetStream.Seek(targetResource.Size, SeekOrigin.Begin);
                }

                _notifiables.ForEach(
                    notifiable => notifiable.NotifyImpendingSync(
                        sourceResource,
                        canResume
                            ? targetResource
                            : null
                    )
                );

                var composition = ComposePipeline(sourceStream, targetStream);
                composition.Drain();
            });
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
    }
}