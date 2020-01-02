using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using bitsplat.Storage;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests.TestingSupport
{
    public class ArenaFile : BasicFileResource
    {
        public override string Path { get; }
        public override long Size => Data?.Length ?? 0;
        public override string RelativePath { get; }
        public byte[] Data { get; }

        public ArenaFile(
            string fullPath,
            string relativePath,
            byte[] data)
        {
            Path = fullPath;
            RelativePath = relativePath;
            Data = data;
        }
    }

    public class TestArena : IDisposable
    {
        public IFileSystem SourceFileSystem
            => _sourceFileSystem ??= new LocalFileSystem(SourcePath);

        public IFileSystem TargetFileSystem
            => _targetFileSystem ??= new LocalFileSystem(TargetPath);

        public IFileSystem ArchiveFileSystem
            => _archiveFileSystem ??= new LocalFileSystem(ArchivePath);

        private IFileSystem _sourceFileSystem;
        private IFileSystem _targetFileSystem;
        private IFileSystem _archiveFileSystem;

        public string SourcePath { get; }
        public string TargetPath { get; }
        public string ArchivePath { get; }
        public string ContainerPath => _container.Path;

        private AutoTempFolder _container;

        public TestArena()
        {
            _container = new AutoTempFolder();
            SourcePath = Path.Combine(_container.Path, "source");
            TargetPath = Path.Combine(_container.Path, "target");
            ArchivePath = Path.Combine(_container.Path, "archive");

            new[] { SourcePath, TargetPath, ArchivePath }
                .ForEach(p => Directory.CreateDirectory(p));
        }

        public void Dispose()
        {
            _container?.Dispose();
            _container = null;
        }

        public string CreateResource(
            string basePath,
            string relativePath,
            byte[] data)
        {
            var fullPath = Path.Combine(basePath, relativePath);
            File.WriteAllBytes(
                fullPath,
                data);
            return fullPath;
        }

        public string CreateSourceResource(
            string relativePath,
            byte[] data)
        {
            return CreateResource(
                SourcePath,
                relativePath,
                data);
        }

        public string CreateTargetResource(
            string relativePath,
            byte[] data)
        {
            return CreateResource(
                TargetPath,
                relativePath,
                data);
        }

        public string CreateTargetFolder(params string[] folderPath)
        {
            var fullPath = Path.Combine(
                new[] { TargetPath }.And(folderPath)
            );
            LocalFileSystem.EnsureFolderExists(fullPath);
            return Path.Combine(folderPath);
        }

        public string SourcePathFor(params string[] relative)
        {
            return Path.Combine(
                SourcePath
                    .AsArray()
                    .Concat(relative)
                    .ToArray()
            );
        }

        public string TargetPathFor(params string[] relative)
        {
            return Path.Combine(
                TargetPath
                    .AsArray()
                    .Concat(relative)
                    .ToArray()
            );
        }

        public string ArchivePathFor(params string[] relative)
        {
            return Path.Combine(
                ArchivePath
                    .AsArray()
                    .Concat(relative)
                    .ToArray()
            );
        }

        public ArenaFile CreateSourceFile(
            string path = null,
            byte[] data = null)
        {
            var (subFolder, name) = GrokPathParts(path);
            return CreateFile(
                SourcePath,
                subFolder,
                name,
                data);
        }

        public ArenaFile CreateTargetFile(
            string path = null,
            byte[] data = null)
        {
            var (subFolder, name) = GrokPathParts(path);
            return CreateFile(
                TargetPath,
                subFolder,
                name,
                data
            );
        }

        private (string subFolder, string name) GrokPathParts(
            string path)
        {
            var subFolder = null as string;
            var name = null as string;
            if (path is null)
            {
                return (subFolder, name);
            }

            var parts = Regex.Split(path, "[/|\\\\]");
            if (parts.Length > 1)
            {
                subFolder = parts.Take(
                        parts.Length - 1
                    )
                    .JoinWith(Path.DirectorySeparatorChar.ToString());
                name = parts.Last();
            }
            else
            {
                name = parts.First();
            }

            return (subFolder, name);
        }

        private static ArenaFile CreateFile(
            string baseFolder,
            string subFolder = null,
            string name = null,
            byte[] data = null)
        {
            data ??= GetRandomBytes();
            name ??= GetRandomString(4);
            var path = CombinePaths(baseFolder, subFolder, name);
            var containingFolder = Path.GetDirectoryName(path);
            LocalFileSystem.EnsureFolderExists(containingFolder);
            
            File.WriteAllBytes(path, data);
            return new ArenaFile(
                path,
                CombinePaths(subFolder, name),
                data);
        }

        private static string CombinePaths(params string[] elements)
        {
            return Path.Combine(
                elements
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray()
            );
        }
    }
}