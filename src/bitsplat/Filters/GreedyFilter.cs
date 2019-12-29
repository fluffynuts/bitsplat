using System.Collections.Generic;
using System.Linq;
using bitsplat.History;
using bitsplat.Storage;

namespace bitsplat.Filters
{
    public class GreedyFilter : IFilter
    {
        public FilterResult Filter(
            IFileResource sourceResource,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository)
        {
            return HaveMatchingTargetResource() ||
                   HaveMatchingHistoryItem()
                       ? FilterResult.Ambivalent
                       : FilterResult.Include;

            bool HaveMatchingTargetResource()
            {
                var existing = targetResources.FirstOrDefault(
                    r => r.RelativePath == sourceResource.RelativePath
                );
                return existing?.Size == sourceResource.Size;
            }

            bool HaveMatchingHistoryItem()
            {
                var historyItem = targetHistoryRepository.Find(
                    sourceResource.RelativePath
                );
                return historyItem?.Size == sourceResource.Size;
            }
        }
    }
}