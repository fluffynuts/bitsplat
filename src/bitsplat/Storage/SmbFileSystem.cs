using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCifs.Smb;

namespace bitsplat.Storage
{
    public class SmbFileSystem: IFileSystem
    {
        public string BasePath { get; }

        public SmbFileSystem(string basePath)
        {
            BasePath = basePath 
                ?? throw new ArgumentNullException(nameof(basePath));
            
            if (!BasePath.EndsWith("/"))
            {
                BasePath += "/";
            }
        }

        public bool Exists(string path)
        {
            var file = EntryFor(path);
            return file.Exists();
        }

        private SmbFile EntryFor(string path)
        {
            return new SmbFile($"{BasePath}{path}");
        }

        public bool IsFile(string path)
        {
            var file = EntryFor(path);
            return file.IsFile();
        }

        public bool IsDirectory(string path)
        {
            var file = EntryFor(path);
            return file.IsDirectory();
        }

        public Stream Open(string path, FileMode mode, FileAccess fileAccess)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            var entry = new SmbFile(BasePath);
            return entry.ListFiles()
                .Select(WrapAsReadWriteFileResource)
                .ToArray();
        }

        private IReadWriteFileResource WrapAsReadWriteFileResource(SmbFile arg)
        {
            return new SmbReadWriteFileResource(arg, BasePath, this);
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive(ListOptions options)
        {
            throw new System.NotImplementedException();
        }

        public long FetchSize(string path)
        {
            throw new System.NotImplementedException();
        }

        public void Delete(string path)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SmbReadWriteFileResource 
        : BasicFileResource, IReadWriteFileResource
    {
        public override string Path { get; }
        public override long Size { get; }
        public override string RelativePath { get; }
        private readonly string _basePath;

        public SmbReadWriteFileResource(
            SmbFile smbFile,
            string basePath,
            IFileSystem fileSystem
        )
        {
            _basePath = basePath;
            _smbFile = smbFile;
            Path = smbFile.GetPath();
        }

        private readonly SmbFile _smbFile;

        public Stream OpenForRead()
        {
            throw new System.NotImplementedException();
        }

        public Stream OpenForWrite()
        {
            throw new System.NotImplementedException();
        }
    }
}