using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public interface ISyncQueueNotifiable
    {
        void NotifySyncBatchStart(
            IEnumerable<IFileResourceProperties> sourceResources);

        void NotifySyncBatchComplete(
            IEnumerable<IFileResourceProperties> sourceResources);

        void NotifySyncStart(
            IFileResourceProperties sourceResource,
            IFileResourceProperties targetResource);

        void NotifySyncComplete(
            IFileResourceProperties sourceResource,
            IFileResourceProperties targetResource);
    }
}