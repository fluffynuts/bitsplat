using System.Collections.Generic;
using System.Linq;
using bitsplat.CommandLine;
using bitsplat.History;
using bitsplat.Storage;

namespace bitsplat.Filters
{
    /// <summary>
    /// opts in a source resource if
    /// - base host folder found at target
    /// - base host folder found in target history db
    /// where 'base host folder' would refer to
    /// 'Some Series'
    /// of
    /// 'Some Series/Season 01'
    /// </summary>
    public class TargetOptInFilter : IFilter
    {
        public FilterResult Filter(
            IFileResource sourceResource,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository,
            IFileSystem source,
            IFileSystem target)
        {
            var primaryAncestor = FindPrimaryAncestorFolder(
                sourceResource.RelativePath
            );
            var shouldRecordOnly =
                ResourceExistsAtTarget(targetResources, sourceResource) ||
                ResourceExistsInHistory(targetHistoryRepository, sourceResource);
            
            if (shouldRecordOnly)
            {
                return FilterResult.RecordOnly;
            }

            var shouldInclude =
                target.IsDirectory(primaryAncestor) ||
                RelativeBaseExistsInHistory(targetHistoryRepository, primaryAncestor);

            return shouldInclude
                       ? FilterResult.Include
                       : FilterResult.Exclude;
        }

        private static bool ResourceExistsAtTarget(
            IEnumerable<IFileResource> targetResources,
            IFileResource sourceResource)
        {
            return targetResources.Any(
                o => o.RelativePath == sourceResource.RelativePath &&
                     o.Size == sourceResource.Size
            );
        }

        private static bool ResourceExistsInHistory(
            ITargetHistoryRepository targetHistoryRepository,
            IFileResource sourceResource)
        {
            return targetHistoryRepository.Find(
                           sourceResource.RelativePath
                       )
                       ?.Size ==
                   sourceResource.Size;
        }

        private static bool RelativeBaseExistsInHistory(
            ITargetHistoryRepository targetHistoryRepository,
            string relativeBase)
        {
            return targetHistoryRepository.FindAll(
                    $"{relativeBase}/*"
                )
                .Any();
        }

        private static string FindPrimaryAncestorFolder(
            string path)
        {
            var parts = path.SplitPath();
            return parts.Length == 1
                       ? ""
                       : parts.First();
        }
    }
}