using System;
using System.Collections.Generic;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;
using PeanutButter.Utils;

namespace bitsplat.Archivers
{
    public interface IArchiver
    {
        /// <returns>relative paths of the files which were marked for archiving but not archived</returns>
        IReadOnlyList<string> RunArchiveOperations(
            IFileSystem target,
            IFileSystem archive,
            IFileSystem source);
    }

    public class ArchiveFileSystems
    {
        public IFileSystem Target { get; }
        public IFileSystem Archive { get; }
        public IFileSystem Source { get; }

        public ArchiveFileSystems(
            IFileSystem target,
            IFileSystem archive,
            IFileSystem source)
        {
            Target = target;
            Archive = archive;
            Source = source;
        }
    }

    public class Mede8erArchiver : IArchiver
    {
        private readonly IPassThrough[] _intermediatePipes;
        private readonly IProgressReporter _progressReporter;
        private readonly IOptions _options;
        private readonly IMessageWriter _messageWriter;
        private readonly IDelayer _delayer;

        // TODO: test if it makes sense to have any pass-through
        // pipes from the caller -- perhaps progress makes sense
        // at least?
        public Mede8erArchiver(
            IPassThrough[] intermediatePipes,
            IProgressReporter progressReporter,
            IOptions options,
            IMessageWriter messageWriter,
            IDelayer delayer
        )
        {
            _delayer = delayer;
            _intermediatePipes = intermediatePipes;
            _progressReporter = progressReporter;
            _options = options;
            _messageWriter = messageWriter;
        }

        public IReadOnlyList<string> RunArchiveOperations(
            IFileSystem target,
            IFileSystem archive,
            IFileSystem source)
        {
            var notArchived = new List<string>();
            var targetResources = target.ListResourcesRecursive();
            var archiveMarkers = targetResources
                .Where(r => r.Name?.EndsWith(".t") ?? false)
                .Select(r => r.RelativePath)
                .ToArray();
            var toArchive = archiveMarkers
                .Select(p => p.RegexReplace(".t$", ""))
                .ToArray();

            // the source is the source of truth: a watched file which has gone from
            // there doesn't need archiving, and stale removal deals with the target copy
            toArchive
                .Where(p => !source.IsFile(p))
                .ForEach(p => _messageWriter.Write(
                    $"Not archiving {p}: not found at source; " +
                    (_options.KeepStaleFiles
                         ? "it will be kept at the target (--keep-stale)"
                         : "it will be removed from the target as stale")
                ));

            var result = SynchronizeArchiveFiles(source, archive, toArchive, _options);
            if (_options.DryRun)
            {
                return notArchived;
            }

            result.Failed.ForEach(r => ReportNotArchived(r.RelativePath, "copy to archive failed"));

            result.Synced.ForEach(r =>
            {
                if (archive.FetchSize(r.RelativePath) != r.Size)
                {
                    ReportNotArchived(r.RelativePath, "not found at archive after copy");
                    return;
                }

                source.Delete(r.RelativePath);
                target.Delete(r.RelativePath);
                target.Delete($"{r.RelativePath}.t");
            });

            return notArchived;

            void ReportNotArchived(string path, string reason)
            {
                notArchived.Add(path);
                _messageWriter.Write(
                    $"Not archiving {path}: {reason}; it has been left at the source and target"
                );
            }
        }

        private SyncResult SynchronizeArchiveFiles(
            IFileSystem source,
            IFileSystem target,
            string[] toArchive,
            IOptions options)
        {
            var archiverFilter = new Mede8erArchiveFilter(
                toArchive
            );

            var synchronizer = new Synchronizer(
                new NullTargetHistoryRepository(),
                new SimpleResumeStrategy(options, _messageWriter),
                _intermediatePipes,
                new IFilter[] { archiverFilter },
                _progressReporter,
                options,
                _delayer
            );

            return synchronizer.Synchronize(
                $"Start archive: {source.BasePath} => {target.BasePath}",
                source,
                target
            );
        }
    }

    public class Mede8erArchiveFilter : IFilter
    {
        private readonly HashSet<string> _archiveFiles;

        public Mede8erArchiveFilter(
            string[] archiveFiles)
        {
            _archiveFiles = new HashSet<string>(
                archiveFiles,
                StringComparer.CurrentCulture
            );
        }

        public FilterResult Filter(
            IFileResource sourceResource,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository,
            IFileSystem source,
            IFileSystem target)
        {
            return _archiveFiles.Contains(sourceResource.RelativePath)
                       ? FilterResult.Include
                       : FilterResult.Exclude;
        }
    }
}