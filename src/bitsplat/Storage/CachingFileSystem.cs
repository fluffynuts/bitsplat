using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace bitsplat.Storage
{
    public class CachingFileSystem : IFileSystem
    {
        public static IFileSystem For(string uri)
        {
            return new CachingFileSystem(
                FileSystem.For(uri)
            );
        }

        public string BasePath => _underlying.BasePath;

        private readonly IFileSystem _underlying;

        private readonly ConcurrentDictionary<string, object> _cache
            = new ConcurrentDictionary<string, object>();

        public CachingFileSystem(IFileSystem underlying)
        {
            _underlying = underlying;
        }

        public bool Exists(string path)
        {
            return Resolve(
                $"{nameof(Exists)}-{path}",
                () => _underlying.Exists(path)
            );
        }

        public bool IsFile(string path)
        {
            return Resolve(
                $"{nameof(IsFile)}-{path}",
                () => _underlying.IsFile(path)
            );
        }

        public bool IsDirectory(string path)
        {
            return Resolve(
                $"{nameof(IsDirectory)}-{path}",
                () => _underlying.IsDirectory(path)
            );
        }

        public Stream Open(string path, FileMode mode)
        {
            return _underlying.Open(path, mode);
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            return Resolve(
                nameof(ListResourcesRecursive),
                () => _underlying.ListResourcesRecursive()
            );
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive(
            ListOptions options)
        {
            return Resolve(
                $"{nameof(ListResourcesRecursive)}-{options.GetHashCode()}",
                () => _underlying.ListResourcesRecursive(options)
            );
        }

        public long FetchSize(string path)
        {
            return Resolve(
                $"{nameof(FetchSize)}-{path}",
                () => _underlying.FetchSize(path)
            );
        }

        public void Delete(string path)
        {
            _underlying.Delete(path);
        }

        private T Resolve<T>(
            string cacheKey,
            Func<T> generator)
        {
            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                return (T) cachedResult;
            }

            var result = generator();
            _cache[cacheKey] = result;
            return result;
        }
    }
}