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
        private readonly IMessageWriter _messageWriter;

        public FileSystemFactory(IMessageWriter messageWriter)
        {
            _messageWriter = messageWriter;
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
                    _messageWriter
                );
            }

            throw new NotSupportedException(
                $"Protocol not supported: {u.Scheme}"
            );
        }
    }
}