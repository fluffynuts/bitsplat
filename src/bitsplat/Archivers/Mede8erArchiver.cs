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
        void RunArchiveOperations(
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

        // TODO: test if it makes sense to have any pass-through
        // pipes from the caller -- perhaps progress makes sense
        // at least?
        public Mede8erArchiver(
            IPassThrough[] intermediatePipes,
            IProgressReporter progressReporter
        )
        {
            _intermediatePipes = intermediatePipes;
            _progressReporter = progressReporter;
        }

        public void RunArchiveOperations(
            IFileSystem target,
            IFileSystem archive,
            IFileSystem source)
        {
            var targetResources = target.ListResourcesRecursive();
            var archiveMarkers = targetResources
                .Where(r => r.Name?.EndsWith(".t") ?? false)
                .Select(r => r.RelativePath)
                .ToArray();
            var toArchive = archiveMarkers
                .Select(p => p.RegexReplace(".t$", ""))
                .ToArray();
            
            SynchronizeArchiveFiles(source, archive, toArchive);
            toArchive.ForEach(source.Delete);
            toArchive.ForEach(target.Delete);
            archiveMarkers.ForEach(target.Delete);
        }

        private void SynchronizeArchiveFiles(
            IFileSystem source,
            IFileSystem target,
            string[] toArchive)
        {
            var archiverFilter = new Mede8erArchiveFilter(
                toArchive
            );

            var synchronizer = new Synchronizer(
                new NullTargetHistoryRepository(),
                new AlwaysResumeWhenTargetSmallerStrategy(),
                _intermediatePipes,
                new IFilter[] { archiverFilter },
                _progressReporter
            );

            synchronizer.Synchronize(
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
            ITargetHistoryRepository targetHistoryRepository)
        {
            return _archiveFiles.Contains(sourceResource.RelativePath)
                       ? FilterResult.Include
                       : FilterResult.Exclude;
        }
    }
}