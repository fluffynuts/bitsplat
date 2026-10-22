using System.Collections.Generic;
using System.Linq;
using bitsplat.Archivers;
using bitsplat.Pipes;
using bitsplat.Storage;

namespace bitsplat.StaleFileRemovers
{
    public interface IStaleFileRemover
    {
        void RemoveStaleFiles(
            IFileSystem source,
            IFileSystem target);
    }

    public class NullStaleFileRemover : IStaleFileRemover
    {
        public void RemoveStaleFiles(IFileSystem source, IFileSystem target)
        {
        }
    }

    public class DefaultStaleFileRemover : IStaleFileRemover
    {
        private readonly IProgressReporter _progressReporter;
        private readonly IOptions _options;
        private readonly IMessageWriter _messageWriter;

        public DefaultStaleFileRemover(
            IProgressReporter progressReporter,
            IOptions options,
            IMessageWriter messageWriter)
        {
            _progressReporter = progressReporter;
            _options = options;
            _messageWriter = messageWriter;
        }

        public void RemoveStaleFiles(IFileSystem source, IFileSystem target)
        {
            var sourcePaths = source.ListResourcesRecursive()
                .Select(o => o.RelativePath)
                .AsHashSet();
            target.ListResourcesRecursive()
                .Where(o => !sourcePaths.Contains(o.RelativePath))
                .Where(o => !IsWatchedMarkerForFileAtSource(o.RelativePath, sourcePaths))
                .ForEach(o =>
                {
                    if (_options.DryRun)
                    {
                        _messageWriter.Write($"Would remove target: {o.RelativePath}");
                        return;
                    }

                    _progressReporter.Bookend(
                        $"Remove target: {o.RelativePath}",
                        () => target.Delete(o.RelativePath)
                    );
                });
        }

        // markers are the player's state, created at the target: they're only stale
        // once the file they mark has gone from the source
        private static bool IsWatchedMarkerForFileAtSource(
            string path,
            HashSet<string> sourcePaths)
        {
            const string marker = Mede8erArchiver.WATCHED_MARKER_EXTENSION;
            return path.EndsWith(marker) &&
                   sourcePaths.Contains(path[..^marker.Length]);
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