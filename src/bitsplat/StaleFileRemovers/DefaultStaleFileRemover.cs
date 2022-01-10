using System.Collections.Generic;
using System.Linq;
using bitsplat.Pipes;
using bitsplat.Storage;

namespace bitsplat.StaleFileRemovers
{
    public interface IStaleFileRemover
    {
        void RemoveStaleFiles(
            IFileSystem source,
            IFileSystem target,
            string[] ignore
        );
    }

    public class NullStaleFileRemover : IStaleFileRemover
    {
        public void RemoveStaleFiles(
            IFileSystem source,
            IFileSystem target,
            string[] ignore
        )
        {
        }
    }

    public class DefaultStaleFileRemover : IStaleFileRemover
    {
        private readonly IProgressReporter _progressReporter;

        public DefaultStaleFileRemover(IProgressReporter progressReporter)
        {
            _progressReporter = progressReporter;
        }

        public void RemoveStaleFiles(
            IFileSystem source,
            IFileSystem target,
            string[] ignore
        )
        {
            var sourcePaths = source.ListResourcesRecursive()
                .Select(o => o.RelativePath)
                .AsHashSet();
            target.ListResourcesRecursive()
                .Where(o => !ignore.Contains(o.Path) && !ignore.Contains(o.RelativePath))
                .Where(o => !sourcePaths.Contains(o.RelativePath))
                .ForEach(o =>
                {
                    _progressReporter.Bookend(
                        $"Remove target: {o.RelativePath}",
                        () => target.Delete(o.RelativePath)
                    );
                });
        }
    }

    public static class EnumerableExtensions
    {
        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> collection)
        {
            return new HashSet<T>(collection);
        }
    }
}