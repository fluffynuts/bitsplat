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

        /// <param name="attempt">null when the error happened outside of a sync attempt</param>
        void NotifyError(
            IFileResource sourceResource,
            IFileResource targetResource,
            Exception ex,
            SyncAttempt attempt);

        void NotifyNoWork(
            IFileSystem source,
            IFileSystem target
        );

        void Log(
            string info
        );
    }
}