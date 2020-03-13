using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
using PeanutButter.Utils;

namespace bitsplat.Storage
{
    public class LocalFileSystem : IFileSystem
    {
        public string BasePath => _basePath;
        private readonly string _basePath;
        private readonly IProgressReporter _progressReporter;

        /// <summary>
        /// Creates the LocalFileSystem object with the provided baseFolder from
        ///   which all relative paths are resolved
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="progressReporter"></param>
        public LocalFileSystem(
            string basePath,
            IProgressReporter progressReporter)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(basePath);
            }

            _basePath = basePath;
            _progressReporter = progressReporter;
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
            FileMode mode,
            FileAccess fileAccess)
        {
            var fullPath = FullPathFor(path);
            var containingFolder = Path.GetDirectoryName(fullPath);
            EnsureFolderExists(containingFolder);
            return File.Open(fullPath, mode, fileAccess);
        }

        public static void EnsureFolderExists(string fullPath)
        {
            var current = null as string;
            fullPath.Split(Path.DirectorySeparatorChar.ToString())
                .ForEach(part =>
                {
                    if (current is null)
                    {
                        current = part;
                        if (Platform.IsUnixy &&
                            current == "")
                        {
                            current = "/"; // Path.Combine chucks away a space :/
                        }

                        return;
                    }

                    current = Path.Combine(current, part);
                    if (!Directory.Exists(current))
                    {
                        Directory.CreateDirectory(current);
                    }
                });
        }

        public long FetchSize(string path)
        {
            try
            {
                return new FileInfo(
                    FullPathFor(path)
                ).Length;
            }
            catch
            {
                return -1;
            }
        }

        public void Delete(string path)
        {
            var fullPath = FullPathFor(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private string FullPathFor(
            string possibleRelativePath)
        {
            return possibleRelativePath.StartsWith(_basePath)
                       ? possibleRelativePath
                       : Path.Combine(_basePath, possibleRelativePath);
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive(
            ListOptions options
        )
        {
            return ListResourcesUnder(BasePath, options);
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            return _progressReporter.Bookend(
                $"Listing resources under {BasePath}",
                () => ListResourcesUnder(
                        BasePath,
                        new ListOptions()
                    )
                    .ToArray()
            );
        }

        private IEnumerable<IReadWriteFileResource> ListResourcesUnder(
            string path,
            ListOptions options)
        {
            return Directory.GetFiles(path)
                .Select(p => new LocalReadWriteFileResource(p, BasePath, this))
                .Where(p =>
                    options.IncludeDotFiles ||
                    !p.Name.StartsWith(".")
                )
                .Union(
                    Directory.GetDirectories(path)
                        .SelectMany(
                            dir => ListResourcesUnder(
                                Path.Combine(path, dir),
                                options
                            )
                        )
                );
        }
    }
}