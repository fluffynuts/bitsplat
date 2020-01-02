using System;

namespace bitsplat.Storage
{
    public static class FileSystem
    {
        public static IFileSystem For(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return new NullFileSystem();
            }

            var u = new Uri(uri);
            if (u.Scheme == "file")
            {
                return new LocalFileSystem(
                    u.LocalPath
                );
            }

            throw new NotSupportedException(
                $"Protocol not supported: {u.Scheme}"
            );
        }
    }
}