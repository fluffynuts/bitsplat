using System.Collections.Generic;
using bitsplat.History;
using bitsplat.Storage;

namespace bitsplat.Filters
{
    public class NoDotFilesFilter : IFilter
    {
        public FilterResult Filter(IFileResource sourceResource,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository)
        {
            return sourceResource.FileName().StartsWith(".")
                       ? FilterResult.Exclude
                       : FilterResult.Ambivalent;
        }
    }
}