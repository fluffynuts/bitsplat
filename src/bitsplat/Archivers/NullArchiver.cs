using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat.Archivers
{
    public class NullArchiver : IArchiver
    {
        public IReadOnlyList<string> RunArchiveOperations(
            IFileSystem target,
            IFileSystem archive,
            IFileSystem source)
        {
            return new string[0];
        }
    }
}