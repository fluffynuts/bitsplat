using System;
using System.IO;

namespace bitsplat.Storage
{
    public class LocalReadWriteFileResource
        : BasicFileResource, IReadWriteFileResource
    {
        private readonly string _basePath;
        private readonly IFileSystem _fileSystem;
        public override string Path { get; }
        public override long Size => (_size ?? (_size = FetchSize())).Value;
        public override string RelativePath => _relativePath ?? (_relativePath = GetRelativePath());

        private long? _size;

        private string _relativePath;

        public LocalReadWriteFileResource(
            string path,
            string basePath,
            IFileSystem fileSystem)
        {
            _basePath = basePath;
            _fileSystem = fileSystem;
            Path = path;
        }

        private string GetRelativePath()
        {
            return System.IO.Path.GetRelativePath(_basePath, Path);
        }

        private long FetchSize()
        {
            return _fileSystem.FetchSize(RelativePath);
        }

        public Stream OpenForRead()
        {
            return _fileSystem.Open(
                RelativePath,
                FileMode.Open
            );
        }

        public Stream OpenForWrite()
        {
            return _fileSystem.Open(
                RelativePath,
                FileMode.OpenOrCreate
            );
        }
    }
}