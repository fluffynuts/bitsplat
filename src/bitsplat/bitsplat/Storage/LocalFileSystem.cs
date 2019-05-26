using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace bitsplat.Storage
{
    public class LocalFileSystem : IFileSystem
    {
        public string BasePath => _basePath;
        private readonly string _basePath;

        /// <summary>
        /// Creates the LocalFileSystem object with the provided baseFolder from
        ///   which all relative paths are resolved
        /// </summary>
        /// <param name="basePath"></param>
        public LocalFileSystem(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(basePath);
            }

            _basePath = basePath;
        }

        public bool Exists(string path)
        {
            return IsDirectory(path) || IsFile(path);
        }

        public bool IsFile(string path)
        {
            var fullPath = FullPathFor(path);
            return File.Exists(fullPath);
        }

        public bool IsDirectory(string path)
        {
            var fullPath = FullPathFor(path);
            return Directory.Exists(fullPath);
        }

        public Stream Open(
            string path,
            FileMode mode)
        {
            return File.Open(FullPathFor(path), mode);
        }

        public IEnumerable<IFileResource> ListResourcesRecursive()
        {
            return ListResourcesUnder(BasePath);
        }

        private IEnumerable<IFileResource> ListResourcesUnder(string path)
        {
            return Directory.GetFiles(path)
                .Select(p => new LocalFileResource(p, BasePath))
                .Union(
                    Directory.GetDirectories(path)
                        .SelectMany(dir => ListResourcesUnder(Path.Combine(path, dir)))
                );
        }

        private string FullPathFor(string path)
        {
            return Path.Combine(_basePath, path);
        }
    }
}