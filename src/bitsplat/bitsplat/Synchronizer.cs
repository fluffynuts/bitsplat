namespace bitsplat
{
    public interface ISynchronizer
    {
        void Synchronize(IFileSystem from, IFileSystem to);
    }

    public class Synchronizer
        : ISynchronizer
    {

        public Synchronizer()
        {
        }

        public void Synchronize(
            IFileSystem source,
            IFileSystem target
            )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResources = target.ListResourcesRecursive();
        }
    }
}