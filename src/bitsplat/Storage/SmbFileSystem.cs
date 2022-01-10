using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCifs.Smb;

namespace bitsplat.Storage
{
    public class SmbFileSystem : IFileSystem
    {
        public string BasePath { get; }

        public SmbFileSystem(string basePath)
        {
            BasePath = basePath
                ?? throw new ArgumentNullException(nameof(basePath));

            if (!BasePath.EndsWith("/"))
            {
                BasePath += "/";
            }
        }

        public bool Exists(string path)
        {
            var file = EntryFor(path);
            return file.Exists();
        }

        private SmbFile EntryFor(string path)
        {
            return path.StartsWith(BasePath)
                ? new SmbFile(path)
                : new SmbFile($"{BasePath}{path}");
        }

        public bool IsFile(string path)
        {
            var file = EntryFor(path);
            return file.IsFile();
        }

        public bool IsDirectory(string path)
        {
            var file = EntryFor(path);
            return file.IsDirectory();
        }

        public Stream Open(string path, FileMode mode, FileAccess fileAccess)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            var baseEntry = new SmbFile(BasePath);
            return baseEntry.ListRecursive()
                .Where(IsFile)
                .Select(EntryFor)
                .Select(WrapAsReadWriteFileResource)
                .ToArray();
        }

        private IReadWriteFileResource WrapAsReadWriteFileResource(SmbFile arg)
        {
            return new SmbReadWriteFileResource(arg, BasePath, this);
        }

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive(ListOptions options)
        {
            throw new System.NotImplementedException();
        }

        public long FetchSize(string path)
        {
            throw new System.NotImplementedException();
        }

        public void Delete(string path)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SmbReadWriteFileResource
        : BasicFileResource, IReadWriteFileResource
    {
        public override string Path { get; }
        public override long Size { get; }
        public override string RelativePath { get; }
        private readonly string _basePath;

        public SmbReadWriteFileResource(
            SmbFile smbFile,
            string basePath,
            IFileSystem fileSystem
        )
        {
            _basePath = basePath;
            _smbFile = smbFile;
            Path = smbFile.GetPath();
            if (Path is null)
            {
                throw new ArgumentNullException("file path is null");
            }

            RelativePath = Path.Substring(_basePath.Length);
        }

        private readonly SmbFile _smbFile;

        public Stream OpenForRead()
        {
            throw new System.NotImplementedException();
        }

        public Stream OpenForWrite()
        {
            throw new System.NotImplementedException();
        }
    }

    public static class SmbFileExtensions
    {
        public static string[] ListRecursive(this SmbFile smbFile)
        {
            var basePath = smbFile.GetPath();
            if (smbFile.IsFile())
            {
                throw new ArgumentException($"{basePath} is a file");
            }

            var entries = smbFile.List();
            var result = new List<string>();
            foreach (var entry in entries)
            {
                var entryPath = JoinPath(basePath, entry);
                var s = new SmbFile(entryPath);
                if (!s.Exists())
                {
                    continue; // went missing
                }

                result.Add(s.GetPath());
                if (s.IsDirectory())
                {
                    // underlying lib is picky about folders ending with a /
                    s = new SmbFile($"{entryPath}/");
                    result.AddRange(s.ListRecursive());
                }
            }

            return result.ToArray();
        }

        internal static string JoinPath(
            params string[] parts
        )
        {
            var collected = new List<string>();
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (i == 0)
                {
                    collected.Add(part.TrimEnd('/'));
                }
                else if (i == parts.Length - 1)
                {
                    collected.Add(part.TrimStart('/'));
                }
                else
                {
                    collected.Add(part.Trim('/'));
                }
            }

            return string.Join("/", collected);
        }
    }
}