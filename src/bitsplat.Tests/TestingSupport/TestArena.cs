using System;
using System.IO;
using bitsplat.Storage;
using PeanutButter.Utils;

namespace bitsplat.Tests.TestingSupport
{
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

        private void EnsureFolderExists(string fullPath)
        {
            var current = null as string;
            fullPath.Split(Path.DirectorySeparatorChar.ToString())
                .ForEach(part =>
                {
                    if (string.IsNullOrEmpty(current))
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