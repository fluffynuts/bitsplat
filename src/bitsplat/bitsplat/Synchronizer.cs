using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
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
        private readonly IResumeStrategy _resumeStrategy;
        private readonly IPassThrough[] _intermediatePipes;

        public Synchronizer(
            IResumeStrategy resumeStrategy,
            params IPassThrough[] intermediatePipes)
        {
            _resumeStrategy = resumeStrategy;
            _intermediatePipes = intermediatePipes;
        }

        public void Synchronize(
            IFileSystem source,
            IFileSystem target
        )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResourcesCollection = target.ListResourcesRecursive();
            var targetResources = targetResourcesCollection as IFileResource[]
                ?? targetResourcesCollection.ToArray();
            sourceResources.ForEach(sourceResource =>
            {
                var targetResource = targetResources.FirstOrDefault(
                    r => r.Matches(sourceResource));
                if (targetResource != null)
                {
                    return;
                }

                var sourceStream = sourceResource.Read();
                var targetStream = target.Open(sourceResource.RelativePath, FileMode.OpenOrCreate);
                
                targetResource = targetResources.FirstOrDefault(
                    r => r.RelativePath == sourceResource.RelativePath);
                if (targetResource != null)
                {
                    if (_resumeStrategy.CanResume(
                        sourceStream,
                        targetStream))
                    {
                        sourceStream.Seek(targetResource.Size, SeekOrigin.Begin);
                        targetStream.Seek(targetResource.Size, SeekOrigin.Begin);
                    }
                }
                
                
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

            var composition = _intermediatePipes.Aggregate(
                source.Pipe(new NullPassThrough()),
                (acc, cur) => acc.Pipe(cur)
            );
            return composition.Pipe(target);
        }
    }
}