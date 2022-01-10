using System.Collections.Generic;
using System.IO;

namespace bitsplat.Storage
{
    public class NullFileSystem : IFileSystem
    {
        public string BasePath => "";

        public bool Exists(string path)
        {
            return false;
        }

        public bool IsFile(string path)
        {
            return false;
        }

        public bool IsDirectory(string path)
        {
            return false;
        }

        public Stream Open(
            string path, 
            FileMode mode,
            FileAccess access)
        {
            return new MemoryStream();
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            return new IReadWriteFileResource[0];
        }

        public long FetchSize(string path)
        {
            return -1;
        }

        public void Delete(string path)
        {
        }
    }
}