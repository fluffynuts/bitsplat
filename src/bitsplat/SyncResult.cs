using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat
{
    public class SyncResult
    {
        public static SyncResult NothingAttempted { get; } = new SyncResult(
            new IReadWriteFileResource[0],
            new IReadWriteFileResource[0]
        );

        public IReadOnlyList<IReadWriteFileResource> Synced { get; }
        public IReadOnlyList<IReadWriteFileResource> Failed { get; }

        public SyncResult(
            IReadOnlyList<IReadWriteFileResource> synced,
            IReadOnlyList<IReadWriteFileResource> failed)
        {
            Synced = synced;
            Failed = failed;
        }
    }
}
