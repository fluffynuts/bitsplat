using System;
using System.Collections.Generic;
using System.Linq;
using bitsplat.History;
using bitsplat.Storage;

namespace bitsplat.Filters
{
    public class SimpleTargetExistsFilter : IFilter
    {
        public FilterResult Filter(IFileResource sourceResource,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository,
            IFileSystem source,
            IFileSystem target)
        {
            var shouldExclude =
                TargetFileExists(targetResources, sourceResource) ||
                HistoryFileExists(targetHistoryRepository, sourceResource);
            return shouldExclude
                       ? FilterResult.Exclude
                       : FilterResult.Ambivalent;
        }

        private bool HistoryFileExists(
            ITargetHistoryRepository targetHistoryRepository,
            IFileResource sourceResource)
        {
            var existing = targetHistoryRepository.Find(
                sourceResource.RelativePath
            );
            return existing?.Size == sourceResource.Size;
        }

        private bool TargetFileExists(
            IEnumerable<IFileResource> targetResources,
            IFileResource sourceResource)
        {
            return targetResources.FirstOrDefault(
                           t => t.RelativePath.Equals(
                               sourceResource.RelativePath,
                               StringComparison.CurrentCulture
                           )
                       )
                       ?.Size ==
                   sourceResource.Size;
        }
    }
}