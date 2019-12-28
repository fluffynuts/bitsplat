using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using bitsplat.Storage;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests.TestingSupport
{
    public class ArenaFile: BasicFileResource
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
        public IFileSystem SourceFileSystem { get; }
        public IFileSystem TargetFileSystem { get; }
        public string SourcePath { get; }
        public string TargetPath { get; }
        public string ContainerPath => _container.Path;

        private AutoTempFolder _container;

        public TestArena()
        {
            _container = new AutoTempFolder();
            SourcePath = Path.Combine(_container.Path, "source");
            TargetPath = Path.Combine(_container.Path, "target");
            Directory.CreateDirectory(SourcePath);
            Directory.CreateDirectory(TargetPath);
            SourceFileSystem = new LocalFileSystem(SourcePath);
            TargetFileSystem = new LocalFileSystem(TargetPath);
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
            EnsureFolderExists(fullPath);
            return Path.Combine(folderPath);
        }

        public string SourcePathFor(string relative)
        {
            return Path.Combine(SourcePath, relative);
        }

        public string TargetPathFor(string relative)
        {
            return Path.Combine(TargetPath, relative);
        }

        public ArenaFile CreateSourceFile(
            string path = null,
            byte[] data = null)
        {
            var subFolder = null as string;
            var name = null as string;
            if (path != null)
            {
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
            }

            return CreateFileIn(
                SourcePath,
                subFolder,
                name,
                data);
        }

        public static ArenaFile CreateFileIn(
            string baseFolder,
            string subFolder = null,
            string name = null,
            byte[] data = null)
        {
            data ??= GetRandomBytes();
            name ??= GetRandomString(4);
            var path = CombinePaths(baseFolder, subFolder, name);
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

        private void EnsureFolderExists(string fullPath)
        {
            var current = null as string;
            fullPath.Split(Path.DirectorySeparatorChar.ToString())
                .ForEach(part =>
                {
                    if (string.IsNullOrEmpty(current) ||
                        current.EndsWith(":"))
                    {
                        current = part;
                        if (string.IsNullOrWhiteSpace(current))
                        {
                            return;
                        }
                    }

                    var partial = Path.Combine(current, part);
                    if (!Directory.Exists(partial))
                    {
                        Directory.CreateDirectory(partial);
                    }
                });
        }
    }
}