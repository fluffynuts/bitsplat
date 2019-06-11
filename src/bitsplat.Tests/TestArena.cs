using System;
using System.IO;
using bitsplat.Storage;
using PeanutButter.Utils;

namespace bitsplat.Tests
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
    }
}