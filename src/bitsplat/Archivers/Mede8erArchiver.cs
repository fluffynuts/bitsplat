using System;
using System.Collections.Generic;
using System.IO;
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
            IFileSystem source,
            IFileSystem target
        );
    }

    public class Mede8erArchiver : IArchiver
    {
        private readonly IPassThrough[] _intermediatePipes;

        public Mede8erArchiver()
            : this(new[] { new NullPassThrough() })
        {
        }

        // TODO: test if it makes sense to have any pass-through
        // pipes from the caller -- perhaps progress makes sense
        // at least?
        public Mede8erArchiver(
            IPassThrough[] intermediatePipes
        )
        {
            _intermediatePipes = intermediatePipes;
        }

        public void RunArchiveOperations(
            IFileSystem source,
            IFileSystem target)
        {
            var sourceResources = source.ListResourcesRecursive();
            var archiveMarkers = sourceResources
                .Where(r => r.Name?.EndsWith(".t") ?? false)
                .Select(r => r.Name)
                .ToArray();
            var toArchive = archiveMarkers
                .Select(p => p.RegexReplace(".t$", ""))
                .ToArray();
            SynchronizeArchiveFiles(source, target, toArchive);
            toArchive.ForEach(source.Delete);
            archiveMarkers.ForEach(source.Delete);
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
                new AlwaysResumeStrategy(),
                _intermediatePipes,
                new[] { archiverFilter }
            );

            synchronizer.Synchronize(
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