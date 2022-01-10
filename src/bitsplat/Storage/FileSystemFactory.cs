using System;
using bitsplat.Pipes;

namespace bitsplat.Storage
{
    public interface IFileSystemFactory
    {
        IFileSystem CachingFileSystemFor(string path);
        IFileSystem FileSystemFor(string path);
    }

    public class FileSystemFactory : IFileSystemFactory
    {
        private readonly IProgressReporter _progressReporter;

        public FileSystemFactory(IProgressReporter progressReporter)
        {
            _progressReporter = progressReporter;
        }

        public IFileSystem CachingFileSystemFor(string uri)
        {
            return new CachingFileSystem(
                FileSystemFor(uri)
            );
        }

        public IFileSystem FileSystemFor(
            string uri
        )
        {
            if (string.IsNullOrEmpty(uri))
            {
                return new NullFileSystem();
            }

            var u = new Uri(uri);
            if (u.Scheme == "file")
            {
                return new LocalFileSystem(
                    u.LocalPath,
                    _progressReporter
                );
            }

            if (u.Scheme == "smb")
            {
                return new SmbFileSystem(
                    uri
                );
            }

            throw new NotSupportedException(
                $"Protocol not supported: {u.Scheme}"
            );
        }
    }
}