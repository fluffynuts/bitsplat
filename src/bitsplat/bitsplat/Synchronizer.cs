using System.Collections;
using System.IO;

namespace bitsplat
{
    public interface ISynchronizer
    {
        void Synchronize(IFileSystem from, IFileSystem to);
    }

    public class Synchronizer
        : ISynchronizer
    {
        public void Synchronize(
            IFileSystem source,
            IFileSystem target
            )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResources = target.ListResourcesRecursive();
            sourceResources.ForEach(sourceResource =>
            {
                var sourceStream = sourceResource.Read();
                var targetStream = target.Open(sourceResource.RelativePath, FileMode.OpenOrCreate);
                
            });
        }
    }
}