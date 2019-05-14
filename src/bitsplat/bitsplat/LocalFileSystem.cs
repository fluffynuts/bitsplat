using System;
using System.IO;

namespace bitsplat
{
    public interface IFileSystem
    {
        /// <summary>
        /// Should reflect the base path for which this
        /// filesystem wrapper was created
        /// </summary>
        string BasePath { get; }

        /// <summary>
        /// Tests if a path exists as a file or directory,
        /// relative to the BasePath of the filesystem
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool Exists(string path);
        
        /// <summary>
        /// Tests if a path exists as a file, relative to
        /// the BasePath of the filesystem
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsFile(string path);
        
        /// <summary>
        /// Tests if a path exists as a directory, relative to the
        /// BasePath of the filesystem
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsDirectory(string path);
        
        /// <summary>
        /// Attempts to open a file with the provided path,
        /// relative to the BasePath of the filesystem, with
        /// the required FileMode
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        Stream Open(string path, FileMode mode);
    }

    public class LocalFileSystem: IFileSystem
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

        public Stream Open(string path, FileMode mode)
        {
            return File.Open(FullPathFor(path), mode);
        }

        private string FullPathFor(string path)
        {
            return Path.Combine(_basePath, path);
        }
    }
}