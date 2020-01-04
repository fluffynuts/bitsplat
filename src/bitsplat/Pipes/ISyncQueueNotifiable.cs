using System;
using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public interface ISyncQueueNotifiable
    {
        void NotifySyncBatchStart(
            string label,
            IEnumerable<IFileResource> sourceResources);

        void NotifySyncBatchComplete(
            string label,
            IEnumerable<IFileResource> sourceResources);

        void NotifySyncStart(
            IFileResource sourceResource,
            IFileResource targetResource);

        void NotifySyncComplete(
            IFileResource sourceResource,
            IFileResource targetResource);
        
        void NotifyError(
            IFileResource sourceResource,
            IFileResource targetResource,
            Exception ex);

        void NotifySyncBatchPrepare(
            string label,
            IFileSystem source,
            IFileSystem target);
    }
}