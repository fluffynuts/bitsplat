using bitsplat.Storage;

namespace bitsplat.Archivers
{
    public class NullArchiver : IArchiver
    {
        public void RunArchiveOperations(
            IFileSystem target,
            IFileSystem archive,
            IFileSystem source)
        {
            /* intentionally does nothing */
        }
    }
}