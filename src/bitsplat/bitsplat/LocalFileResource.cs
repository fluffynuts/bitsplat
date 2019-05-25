using System;
using System.IO;

namespace bitsplat
{
    public class LocalFileResource
        : IFileResource
    {
        private readonly string _basePath;
        public string Path { get; }
        public long Size => (_size ?? (_size = FetchSize())).Value;
        public string RelativePath => _relativePath ?? (_relativePath = GetRelativePath());

        private long? _size;

        private string _relativePath;

        public LocalFileResource(
            string path,
            string basePath)
        {
            _basePath = basePath;
            Path = path;
        }

        private string GetRelativePath()
        {
            return System.IO.Path.GetRelativePath(_basePath, Path);
        }

        private long FetchSize()
        {
            try
            {
                return new FileInfo(Path).Length;
            }
            catch
            {
                return -1;
            }
        }

        public Stream Read()
        {
            throw new NotImplementedException();
        }

        public Stream Write()
        {
            throw new NotImplementedException();
        }
    }
}