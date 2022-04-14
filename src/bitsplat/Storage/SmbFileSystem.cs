using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            return new SmbFile(UrlFor(path));
        }

        private string UrlFor(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path may not be null or whitespace", nameof(path));
            }

            return path.StartsWith(BasePath)
                ? path
                : $"{BasePath}{path}";
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

        public IEnumerable<IReadWriteFileResource> ListResourcesRecursive()
        {
            var baseEntry = new SmbFile(BasePath);
            return baseEntry.ListRecursive()
                .Where(IsFile)
                .Select(EntryFor)
                .Select(WrapAsReadWriteFileResource)
                .ToArray();
        }

        public Stream Open(
            string path,
            FileMode mode,
            FileAccess fileAccess
        )
        {
            var entry = EntryFor(path);
            entry.Mkdirs();
            return new SmbFileStream(entry, this, fileAccess == FileAccess.Read);
        }

        public long FetchSize(string path)
        {
            return EntryFor(path).Length();
        }

        public void Delete(string path)
        {
            var entry = EntryFor(path);
            if (!entry.Exists())
            {
                return;
            }
            entry.Delete();
        }

        private IReadWriteFileResource WrapAsReadWriteFileResource(SmbFile arg)
        {
            return new SmbReadWriteFileResource(arg, BasePath, this);
        }
    }

    public class SmbFileStream : Stream
    {
        public SmbFile File { get; }
        public SmbFileSystem FileSystem { get; }
        public bool IsReadOnly { get; }

        public SmbFileStream(
            SmbFile file,
            SmbFileSystem fileSystem,
            bool isReadOnly
        )
        {
            File = file;
            FileSystem = fileSystem;
            IsReadOnly = isReadOnly;
            Position = 0;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            using var readStream = new SmbFileInputStream(File);
            readStream.SetPosition(Position);
            var result = readStream.Read(buffer, offset, count);
            Position += result;
            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var exists = File.Exists();
            using var writeStream = new SmbFileOutputStream(File, File.Exists());
            writeStream.SetPosition(Position);
            writeStream.Write(buffer, offset, count);
            Position += count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position = SeekInternal(offset, origin);
        }

        private long SeekInternal(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.End)
            {
                throw new NotSupportedException(
                    "Only seeking from the beginning or current position of the file is supported");
            }

            var position = origin == SeekOrigin.Begin
                ? offset
                : Position + offset;
            return Position = position;
        }

        public override void SetLength(long value)
        {
            if (value != 0)
            {
                throw new ArgumentException($"{nameof(SetLength)} only supports truncation (ie, length of zero)");
            }

            if (File.Exists())
            {
                File.Delete();
            }
        }

        private long ReadLength()
        {
            return File.Exists() ? File.Length() : 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => !IsReadOnly;
        public override long Length => ReadLength();

        public override long Position { get; set; }
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

    public static class CifsExtensions
    {
        public static void SetPosition(this SmbFileOutputStream smbFile, long newPosition)
        {
            OutputStreamPositionField.SetValue(smbFile, newPosition);
        }

        public static long GetPosition(this SmbFileOutputStream smbFile)
        {
            return (long)OutputStreamPositionField.GetValue(smbFile);
        }

        public static long GetPosition(this SmbFileInputStream smbFile)
        {
            return (long)InputStreamPositionField.GetValue(smbFile);
        }

        public static void SetPosition(this SmbFileInputStream smbFile, long newPosition)
        {
            InputStreamPositionField.SetValue(smbFile, newPosition);
        }

        private static readonly FieldInfo OutputStreamPositionField = typeof(SmbFileOutputStream)
            .GetField("_fp", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo InputStreamPositionField = typeof(SmbFileInputStream)
            .GetField("_fp", BindingFlags.Instance | BindingFlags.NonPublic);

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