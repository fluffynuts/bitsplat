using System.Collections.Generic;
using bitsplat.History;
using bitsplat.Storage;

namespace bitsplat.Filters
{
    public enum FilterResult
    {
        Ambivalent,
        Include,
        Exclude
    }

    /// <summary>
    /// implementations choose whether to filter files in or out
    /// - filters are additive in that if one filter opts
    /// </summary>
    public interface IFilter
    {
        FilterResult Filter(
            IFileResource sourceResource,
            IEnumerable<IFileResource> targetResources,
            ITargetHistoryRepository targetHistoryRepository);
    }
}