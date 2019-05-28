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
        public string SourcePath => _sourceFolder?.Path;
        public string TargetPath => _targetFolder?.Path;

        private AutoTempFolder _sourceFolder;
        private AutoTempFolder _targetFolder;

        public TestArena()
        {
            _sourceFolder = new AutoTempFolder();
            _targetFolder = new AutoTempFolder();
            SourceFileSystem = new LocalFileSystem(_sourceFolder.Path);
            TargetFileSystem = new LocalFileSystem(_targetFolder.Path);
        }

        public void Dispose()
        {
            _sourceFolder?.Dispose();
            _targetFolder?.Dispose();
            _sourceFolder = null;
            _targetFolder = null;
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