using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public interface ISyncQueueNotifiable
    {
        void NotifySyncBatchStart(
            IEnumerable<IFileResource> sourceResources);

        void NotifySyncBatchComplete(
            IEnumerable<IFileResource> sourceResources);

        void NotifySyncStart(
            IFileResource sourceResource,
            IFileResource targetResource);

        void NotifySyncComplete(
            IFileResource sourceResource,
            IFileResource targetResource);
    }
}