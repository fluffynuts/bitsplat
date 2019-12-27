using System.Collections.Generic;
using System.IO;
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
            var relativeBase = sourceResource
                .RelativePath
                .Split("/")
                .First();

            var definitelyInclude = RelativeBaseExistsAtTarget(targetResources, relativeBase) ||
                   RelativeBaseExistsInHistory(targetHistoryRepository, relativeBase);
            return definitelyInclude
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

        private static bool RelativeBaseExistsAtTarget(IEnumerable<IFileResource> targetResources, string relativeBase)
        {
            return targetResources.Any(
                target => target.RelativePath.Split(
                                  Path.DirectorySeparatorChar)
                              .First() ==
                          relativeBase);
        }
    }
}