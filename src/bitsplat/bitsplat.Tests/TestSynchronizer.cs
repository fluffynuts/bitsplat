using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using bitsplat.Storage;
using Castle.Core.Resource;
using static NExpect.Expectations;
using NExpect;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestSynchronizer
    {
        [Test]
        public void ShouldImplement_ISynchronizer()
        {
            // Arrange
            Expect(typeof(Synchronizer))
                .To.Implement<ISynchronizer>();
            // Act
            // Assert
        }

        [TestFixture]
        public class Behavior
        {
            [TestFixture]
            public class WhenPresentedWithTwoEmptyFilesystems
            {
                [Test]
                public void ShouldListRecursiveOnBoth()
                {
                    // Arrange
                    var fs1 = Substitute.For<IFileSystem>();
                    var fs2 = Substitute.For<IFileSystem>();
                    var sut = Create();
                    // Act
                    sut.Synchronize(fs1, fs2);
                    // Assert
                    Expect(fs1)
                        .To.Have.Received(1)
                        .ListResourcesRecursive();
                    Expect(fs2)
                        .To.Have.Received(1)
                        .ListResourcesRecursive();
                }
            }

            [TestFixture]
            public class WhenSourceHasOneFileAndTargetIsEmpty
            {
                [Test]
                public void ShouldCopyTheSourceFileToTarget()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = "some-file.ext";
                        var data = GetRandomBytes();
                        CreateResource(
                            arena.SourcePath,
                            relPath,
                            data);
                        var sut = Create();
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        var inTarget = target.ListResourcesRecursive();
                        Expect(inTarget)
                            .To.Contain.Only(1)
                            .Item();
                        var copied = inTarget.Single();
                        Expect(copied.RelativePath)
                            .To.Equal(relPath);
                        Expect(copied)
                            .To.Have.Data(data);
                    }
                }
            }

            [TestFixture]
            public class WhenSourceHasFileAndTargetHasSameFile
            {
                [Test]
                public void ShouldNotReWriteTheFile()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = GetRandomString(2);
                        var data = GetRandomBytes(100);
                        var targetFilePath = CreateResource(
                            arena.TargetPath,
                            relPath,
                            data);
                        CreateResource(
                            arena.SourcePath,
                            relPath,
                            data);
                        var beforeTest = DateTime.Now;
                        var lastWrite = beforeTest.AddMinutes(GetRandomInt(-100, -1));
                        File.SetLastWriteTime(targetFilePath, lastWrite);
                        
                        var sut = Create();
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        Expect(targetFilePath).To.Be.A.File();
                        var targetInfo = new FileInfo(targetFilePath);
                        Expect(targetInfo.LastWriteTime)
                            .To.Be.Less.Than(beforeTest);
                    }
                }
            }
        }

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
        }

        private static IFileSystem CreateFileSystem(
            string basePath)
        {
            return new LocalFileSystem(basePath);
        }

        private static string CreateResource(
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

        private static MemoryStream CreateMemoryStreamContaining(
            byte[] data)
        {
            var result = new MemoryStream();
            result.Write(data, 0, data.Length);
            return result;
        }

        private static ISynchronizer Create()
        {
            return new Synchronizer();
        }
    }

    public static class FileResourceMatchers
    {
        public static void Data(
            this IHave<IFileResource> have,
            byte[] expected)
        {
            have.AddMatcher(actual =>
            {
                if (!File.Exists(actual.Path))
                {
                    return new MatcherResult(
                        false,
                        () => $"Expected {false.AsNot()}to find file at: {actual.Path}");
                }

                using (var stream = actual.Read())
                {
                    var data = stream.ReadAllBytes();
                    var passed = expected.Length == data.Length &&
                                 data.DeepEquals(expected);
                    return new MatcherResult(
                        passed,
                        () =>
                        {
                            var actualHash = data.ToMD5String();
                            var expectedHash = expected.ToMD5String();
                            return $@"Expected {
                                    passed.AsNot()
                                } to find data with hash/length {
                                    expectedHash
                                }{
                                    expected.Length
                                }, but got {
                                    actualHash
                                }/{
                                    data.Length
                                }";
                        });
                }
            });
        }
    }
}