using System.Collections.Generic;
using System.Linq;
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
            ITargetHistoryRepository targetHistoryRepository)
        {
            var primaryAncestor = FindPrimaryAncestorFolder(
                sourceResource.RelativePath
            );
            var shouldInclude =
                RelativeBaseExistsAtTarget(targetResources, primaryAncestor) ||
                RelativeBaseExistsInHistory(targetHistoryRepository, primaryAncestor);
            return shouldInclude
                       ? FilterResult.Include
                       : FilterResult.Ambivalent; // allow another filter to opt-in
        }

        private static bool RelativeBaseExistsInHistory(ITargetHistoryRepository targetHistoryRepository,
            string relativeBase)
        {
            return targetHistoryRepository.FindAll(
                    $"{relativeBase}/*"
                )
                .Any();
        }

        private static bool RelativeBaseExistsAtTarget(
            IEnumerable<IFileResource> targetResources,
            string primaryAncestor)
        {
            // opts in if the primaryAncestor matches any ancestor
            // in targetResources
            // -> root files are always matched
            // -> source Foo/Bar/file.ext matches Foo/*
            return targetResources.Any(
                target => FindPrimaryAncestorFolder(target.RelativePath) ==
                          primaryAncestor);
        }

        private static string FindPrimaryAncestorFolder(
            string path)
        {
            var parts = path.Split("/");
            return parts.Length == 1
                       ? ""
                       : parts.First();
        }
    }
}