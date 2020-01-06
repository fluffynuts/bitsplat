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
        private readonly IMessageWriter _messageWriter;
        public string BasePath => _basePath;
        private readonly string _basePath;

        /// <summary>
        /// Creates the LocalFileSystem object with the provided baseFolder from
        ///   which all relative paths are resolved
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="messageWriter"></param>
        public LocalFileSystem(
            string basePath,
            IMessageWriter messageWriter)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(basePath);
            }

            _basePath = basePath;
            _messageWriter = messageWriter;
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
            var fullPath = FullPathFor(path);
            var containingFolder = Path.GetDirectoryName(fullPath);
            EnsureFolderExists(containingFolder);
            return File.Open(fullPath, mode);
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
            return Run($"Listing resources under {BasePath}",
                () => ListResourcesUnder(BasePath, options)
            );
        }

        private T Run<T>(string message, Func<T> toRun)
        {
            _messageWriter.Rewrite($"{message} ...");
            try
            {
                var result = toRun();
                _messageWriter.Write($"{message} ... done!");
                return result;
            }
            catch
            {
                _messageWriter.Write($"{message} ... failed!");
                throw;
            }
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            return ListResourcesUnder(BasePath, new ListOptions());
        }

        private IEnumerable<IReadWriteFileResource> ListResourcesUnder(
            string path,
            ListOptions options)
        {
            var result = Directory.GetFiles(path)
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
            return result;
        }
    }
}