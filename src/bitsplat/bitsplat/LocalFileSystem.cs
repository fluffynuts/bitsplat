using System;
using System.IO;

namespace bitsplat
{
    public interface IFileSystem
    {
        bool Exists(string path);
        bool IsFile(string path);
        bool IsDirectory(string path);
        
        Stream Open(string path, FileMode mode);
    }

    public class LocalFileSystem: IFileSystem
    {
        private readonly string _baseFolder;

        /// <summary>
        /// Creates the LocalFileSystem object with the provided baseFolder from
        ///   which all relative paths are resolved
        /// </summary>
        /// <param name="baseFolder"></param>
        public LocalFileSystem(string baseFolder)
        {
            if (!Directory.Exists(baseFolder))
            {
                throw new DirectoryNotFoundException(baseFolder);
            }

            _baseFolder = baseFolder;
        }

        public bool Exists(string path)
        {
            throw new NotImplementedException();
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

        public Stream Open(string path, FileMode mode)
        {
            return File.Open(FullPathFor(path), mode);
        }

        private string FullPathFor(string path)
        {
            return Path.Combine(_baseFolder, path);
        }
    }
}