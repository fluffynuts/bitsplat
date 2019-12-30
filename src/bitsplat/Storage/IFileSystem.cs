using System.Collections.Generic;
using System.IO;

namespace bitsplat.Storage
{
    public interface ISourceFileSystem : IFileSystem
    {
    }

    public interface ITargetFileSystem : IFileSystem
    {
    }

    public interface IArchiveFileSystem : IFileSystem
    {
    }

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
        
        /// <summary>
        /// Lists all Files under the base path
        /// </summary>
        /// <returns></returns>
        IEnumerable<IReadWriteFileResource> ListResourcesRecursive();

        /// <summary>
        /// Fetches the size, in bytes, of the given path
        /// - should return -1 if the file is not found
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        long FetchSize(string path);
        
        /// <summary>
        /// Deletes the file given by relative path, if it exists
        /// - does not throw if file does not exist
        /// </summary>
        /// <param name="path"></param>
        void Delete(string path);
    }
}