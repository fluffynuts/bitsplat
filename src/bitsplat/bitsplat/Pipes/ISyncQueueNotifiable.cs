using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public interface ISyncQueueNotifiable
    {
        void NotifySyncBatch(
            IEnumerable<IFileResource> sourceResources
        );

        void NotifyImpendingSync(
            IFileResource sourceResource,
            IFileResource targetResource);
    }
}