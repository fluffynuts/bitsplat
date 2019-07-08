using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.History;
using bitsplat.Storage;

namespace bitsplat
{
    public interface IFilter
    {
        IEnumerable<IFileResource> Filter(
            IEnumerable<IFileResource> sourceResources,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository);
    }

    public class OptInFilter : IFilter
    {
        public IEnumerable<IFileResource> Filter(
            IEnumerable<IFileResource> sourceResources,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository)
        {
            return sourceResources.Where(
                source =>
                {
                    var relativeBase = source
                        .RelativePath
                        .Split("/")
                        .First();

                    return RelativeBaseExistsAtTarget(targetResources, relativeBase) || 
                           RelativeBaseExistsInHistory(targetHistoryRepository, relativeBase);
                });
        }

        private static bool RelativeBaseExistsInHistory(ITargetHistoryRepository targetHistoryRepository, string relativeBase)
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