using System;
using System.IO;
using System.Linq;
using bitsplat.Tests.TestingSupport;
using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    [Explicit("WIP")]
    public class TestProgram
    {
        [Test]
        public void ShouldSyncOneFileInRoot()
        {
            // Arrange
            using (var arena = CreateArena())
            {
                var newFile = arena.CreateSourceFile();
                Expect(newFile.FullPath)
                    .To.Exist();
                var args = new[]
                {
                    "-s",
                    arena.Source.Path,
                    "-t",
                    arena.Target.Path
                };
                var expected = arena.TargetPath(newFile.RelativePath);
                // Act
                Program.Main(args);
                // Assert
                Expect(expected)
                    .To.Exist();
                var data = File.ReadAllBytes(expected);
                Expect(data)
                    .To.Equal(newFile.Data);
            }
        }

        private static TestArena CreateArena()
        {
            return new TestArena();
        }

        public class TestArena : IDisposable
        {
            public class ArenaFile
            {
                public string FullPath { get; }
                public string RelativePath { get; }
                public byte[] Data { get; }

                public ArenaFile(
                    string fullPath,
                    string relativePath,
                    byte[] data)
                {
                    FullPath = fullPath;
                    RelativePath = relativePath;
                    Data = data;
                }
            }

            public AutoTempFolder Target { get; set; }
            public AutoTempFolder Source { get; private set; }

            public TestArena()
            {
                Source = new AutoTempFolder();
                Target = new AutoTempFolder();
            }

            public string SourcePath(string relative)
            {
                return RelativePath(Source, relative);
            }

            public string TargetPath(string relative)
            {
                return RelativePath(Target, relative);
            }

            public string RelativePath(
                AutoTempFolder folder,
                string relative)
            {
                return Path.Combine(folder.Path, relative);
            }

            public ArenaFile CreateSourceFile()
            {
                return CreateFileIn(Source);
            }

            public ArenaFile CreateFileIn(
                AutoTempFolder folder,
                string subFolder = null)
            {
                return CreateFileIn(
                    folder.Path,
                    subFolder);
            }

            public static ArenaFile CreateFileIn(
                string baseFolder,
                string subFolder)
            {
                var data = GetRandomBytes();
                var name = GetRandomString(4);
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

            public void Dispose()
            {
                Target?.Dispose();
                Target = null;
                Source?.Dispose();
                Source = null;
            }
        }
    }
}