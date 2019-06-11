using System;
using System.IO;

namespace bitsplat.Storage
{
    public class LocalFileResource
        : IFileResource
    {
        private readonly string _basePath;
        private readonly IFileSystem _fileSystem;
        public string Path { get; }
        public long Size => (_size ?? (_size = FetchSize())).Value;
        public string RelativePath => _relativePath ?? (_relativePath = GetRelativePath());

        private long? _size;

        private string _relativePath;

        public LocalFileResource(
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

        public Stream Read()
        {
            return _fileSystem.Open(
                RelativePath,
                FileMode.Open
            );
        }

        public Stream Write()
        {
            return _fileSystem.Open(
                RelativePath,
                FileMode.OpenOrCreate
            );
        }
    }
}