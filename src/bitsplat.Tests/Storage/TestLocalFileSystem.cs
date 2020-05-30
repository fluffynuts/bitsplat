using System;
using System.IO;
using System.Linq;
using bitsplat.CommandLine;
using bitsplat.Pipes;
using bitsplat.Storage;
using bitsplat.Tests.TestingSupport;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using PeanutButter.Utils;
using static NExpect.Expectations;

// ReSharper disable PossibleMultipleEnumeration

namespace bitsplat.Tests.Storage
{
    [TestFixture]
    public class TestLocalFileSystem
    {
        [Test]
        public void ShouldImplement_IFileSystem()
        {
            // Arrange
            // Act
            Expect(typeof(LocalFileSystem))
                .To.Implement<IFileSystem>();
            // Assert
        }

        [Test]
        public void Ctor_WhenBasePathDoesNotExist_ShouldAttemptToCreateIt()
        {
            // Arrange
            using var folder = new AutoTempFolder();
            var baseFolder = Path.Combine(folder.Path,
                Guid.NewGuid()
                    .ToString());
            Expect(baseFolder)
                .Not.To.Exist();
            // Act
            Expect(() => Create(baseFolder))
                .To.Throw<DirectoryNotFoundException>();
            // Assert
        }

        [TestFixture]
        public class IsFile
        {
            [Test]
            public void WhenFileExists_RelativeToConstructionPath_ShouldReturnTrue()
            {
                // Arrange
                using var tempFile = new AutoTempFile();
                var container = Path.GetDirectoryName(tempFile.Path);
                var sut = Create(container);
                // Act
                var result = sut.IsFile(tempFile.Path);
                // Assert
                Expect(result)
                    .To.Be.True();
            }

            [Test]
            public void WhenFileDoesNotExist_RelativeToCtorPath_ShouldReturnFalse()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var test = Path.Combine(tempFolder.Path,
                    Guid.NewGuid()
                        .ToString());
                var sut = Create(tempFolder);
                // Act
                var result = sut.IsFile(test);
                // Assert
                Expect(result)
                    .To.Be.False();
            }
        }

        [TestFixture]
        public class IsDirectory
        {
            [Test]
            public void WhenDirectoryExists_RelativeToCtorPath_ShouldReturnTrue()
            {
                // Arrange
                using var tempDir = new AutoTempFolder();
                var test = Guid.NewGuid()
                    .ToString();
                var sub = Path.Combine(tempDir.Path, test);
                Directory.CreateDirectory(sub);
                Expect(sub)
                    .To.Be.A.Directory();
                var sut = Create(tempDir.Path);
                // Act
                var result = sut.IsDirectory(test);
                // Assert
                Expect(result)
                    .To.Be.True();
            }

            [Test]
            public void WhenDirectoryDoesNotExist_RelativeToCtorPath_ShouldReturnFalse()
            {
                // Arrange
                using var tempDir = new AutoTempFolder();
                var test = Guid.NewGuid()
                    .ToString();
                var sub = Path.Combine(tempDir.Path, test);
                Expect(sub)
                    .Not.To.Exist();
                var sut = Create(tempDir.Path);
                // Act
                var result = sut.IsDirectory(test);
                // Assert
                Expect(result)
                    .To.Be.False();
            }
        }

        [TestFixture]
        public class Exists
        {
            [Test]
            public void WhenFileExists_ShouldReturnTrue()
            {
                // Arrange
                using var tempFile = new AutoTempFile();
                var sut = Create(Path.GetDirectoryName(tempFile.Path));
                // Act
                var result = sut.Exists(tempFile.Path);
                // Assert
                Expect(result)
                    .To.Be.True();
            }

            [Test]
            public void WhenDirectoryExists_ShouldReturnTrue()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var sut = Create(tempFolder.Path);
                // Act
                var result = sut.Exists(tempFolder.Path);
                // Assert
                Expect(result)
                    .To.Be.True();
            }

            [Test]
            public void WhenNothingFound_ShouldReturnFalse()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var sut = Create(tempFolder);
                // Act
                var result = sut.Exists(Guid.NewGuid()
                    .ToString());
                // Assert
                Expect(result)
                    .To.Be.False();
            }
        }

        [TestFixture]
        public class Open
        {
            [Test]
            public void WhenFileDoesNotExist_AndModeIs_OpenOrCreate_ShouldCreateFile()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var fileName = Guid.NewGuid()
                    .ToString();
                Expect(Path.Combine(tempFolder.Path, fileName))
                    .Not.To.Exist();
                var expected = GetRandomBytes(1024);
                var sut = Create(tempFolder);
                // Act
                using (var stream = sut.Open(
                    fileName,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite))
                {
                    stream.Write(expected);
                }

                // Assert
                var written = File.ReadAllBytes(
                    Path.Combine(
                        tempFolder.Path,
                        fileName
                    )
                );
                Expect(written)
                    .To.Equal(expected);
            }

            [Test]
            public void WhenFileDoesNotExist_AndModeIs_Append_ShouldCreateFile()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var fileName = Guid.NewGuid()
                    .ToString();
                Expect(Path.Combine(tempFolder.Path, fileName))
                    .Not.To.Exist();
                var expected = GetRandomBytes(1024);
                var sut = Create(tempFolder);
                // Act
                using (var stream = sut.Open(
                    fileName,
                    FileMode.Append,
                    FileAccess.Write))
                {
                    stream.Write(expected);
                }

                // Assert
                var written = File.ReadAllBytes(
                    Path.Combine(
                        tempFolder.Path,
                        fileName
                    )
                );
                Expect(written)
                    .To.Equal(expected);
            }
        }

        [TestFixture]
        public class ListRecursive
        {
            [Test]
            public void ShouldReturnEmptyCollectionForEmptyBase()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var sut = Create(tempFolder);
                // Act
                var results = sut.ListResourcesRecursive();
                // Assert
                Expect(results)
                    .Not.To.Be.Null();
                Expect(results)
                    .To.Be.Empty();
            }

            [Test]
            public void ShouldReturnSingleFileUnderBasePath()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var filePath = tempFolder.CreateRandomFile();
                var sut = Create(tempFolder);
                // Act
                var results = sut.ListResourcesRecursive();
                // Assert
                Expect(results)
                    .To.Contain.Exactly(1)
                    .Matched.By(r => r.Path == filePath);
            }

            [Test]
            public void FirstLevelResultsShouldHaveCorrectRelativePath()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var path = tempFolder.CreateRandomFile();
                var expected = Path.GetFileName(path);
                var sut = Create(tempFolder);
                // Act
                var results = sut.ListResourcesRecursive();
                // Assert
                Expect(results)
                    .To.Contain.Exactly(1)
                    .Matched.By(r => r.RelativePath == expected);
            }

            [Test]
            public void ShouldIgnoreEmptyFoldersUnderBase()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var path = tempFolder.CreateRandomFolder();
                var sut = Create(tempFolder);
                Expect(path)
                    .To.Exist();
                // Act
                var results = sut.ListResourcesRecursive();
                // Assert
                Expect(results)
                    .To.Be.Empty();
            }

            [Test]
            public void ShouldListResourcesUnderFolders()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var file1 = tempFolder.CreateRandomFile();
                var sub1 = tempFolder.CreateRandomFolder();
                var file2 = Path.Combine(sub1, CreateRandomFileIn(sub1));
                var file3 = Path.Combine(sub1, CreateRandomFileIn(sub1));
                var sub2 = Path.Combine(sub1, CreateRandomFolderIn(sub1));
                var file4 = Path.Combine(sub2, CreateRandomFileIn(sub2));

                new[]
                {
                    file1,
                    sub1,
                    file2,
                    file3,
                    sub2,
                    file4
                }.ForEach(o => Expect(o)
                    .To.Exist());
                var sut = Create(tempFolder);
                // Act
                var result = sut.ListResourcesRecursive();
                // Assert
                Expect(result)
                    .To.Contain.Only(4)
                    .Items();
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.Path == file1);
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.Path == file2);
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.Path == file3);
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.Path == file4);
            }

            [Test]
            public void ShouldIgnoreDotFilesByDefault()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                var fileName = ".{GetRandomString(1)}";
                File.WriteAllBytes(
                    Path.Combine(folder.Path, fileName),
                    GetRandomBytes()
                );
                var sut = Create(folder);
                // Act
                var result = sut.ListResourcesRecursive();
                // Assert
                Expect(result)
                    .To.Be.Empty();
            }

            [Test]
            public void ShouldSupplyRelativePathOnDemand()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var file1 = tempFolder.CreateRandomFile();
                var sub = tempFolder.CreateRandomFolder();
                var file2 = Path.Combine(sub, CreateRandomFileIn(sub));
                var expected1 = Path.GetRelativePath(tempFolder.Path, file1);
                var expected2 = Path.GetRelativePath(tempFolder.Path, file2);
                var sut = Create(tempFolder);
                // Act
                var results = sut.ListResourcesRecursive();
                // Assert
                Expect(results)
                    .To.Contain.Only(2)
                    .Items();
                Expect(results)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.RelativePath == expected1 &&
                        o.Path == file1);
                Expect(results)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.RelativePath == expected2 &&
                        o.Path == file2);
            }

            [Test]
            public void ShouldSupplySizeOnDemand()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var file = tempFolder.CreateRandomFile();
                var stat = new FileInfo(file);
                var newData = GetRandomBytes((int) stat.Length + 1, (int) stat.Length + 100);
                var sut = Create(tempFolder);
                // Act
                var results = sut.ListResourcesRecursive();
                File.WriteAllBytes(file, newData);
                // Assert
                Expect(results.Single()
                        .Size)
                    .To.Equal(newData.Length);
            }

            [Test]
            public void ShouldSupplyMinusOneForSizeIfUnableToStat()
            {
                // Arrange
                using var tempFolder = new AutoTempFolder();
                var file = tempFolder.CreateRandomFile();
                var stat = new FileInfo(file);
                var sut = Create(tempFolder);
                // Act
                var results = sut.ListResourcesRecursive();
                File.Delete(file);
                // Assert
                Expect(results.Single()
                        .Size)
                    .To.Equal(-1);
            }
        }

        [TestFixture]
        public class Delete
        {
            [TestFixture]
            public class WhenFileDoesNotExist
            {
                [Test]
                public void ShouldNotThrow()
                {
                    // Arrange
                    using var folder = new AutoTempFolder();
                    var relPath = GetRandomFileName();
                    var fullPath = Path.Combine(folder.Path, relPath);
                    Expect(fullPath)
                        .Not.To.Exist();
                    var sut = Create(folder.Path);
                    // Act
                    Expect(() => sut.Delete(relPath))
                        .Not.To.Throw();
                    // Assert
                }
            }

            [TestFixture]
            public class WhenFileDoesExist
            {
                [Test]
                public void ShouldDeleteIt()
                {
                    // Arrange
                    using var folder = new AutoTempFolder();
                    var relPath = GetRandomFileName();
                    var fullPath = Path.Combine(folder.Path, relPath);
                    File.WriteAllBytes(fullPath, GetRandomBytes());
                    var sut = Create(folder.Path);
                    // Act
                    sut.Delete(relPath);
                    // Assert
                    Expect(fullPath)
                        .Not.To.Exist();
                }
            }
        }

        [TestFixture]
        public class OpeningTheSameFileForReading
        {
            [Test]
            public void CanItBeDone()
            {
                // Arrange
                using var file = new AutoTempFile()
                {
                    StringData = GetRandomString(8196, 16384)
                };
                // Act
                // Assert
            }
        }

        [Explicit("discovery")]
        [Test]
        public void StrangeFile()
        {
            // Arrange
            var path = "/mnt/mede8er-smb/movies/Dragonheart 3 - The Sorcerer's Curse (2015).mkv";
            var basePath = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var fs = new LocalFileSystem(basePath, Substitute.For<IProgressReporter>());
            // Act
            var resource = new LocalReadWriteFileResource(
                fileName,
                basePath,
                fs
            );
            // Assert
            Console.WriteLine(resource.Size);
        }

        private static IFileSystem Create(string baseFolder)
        {
            return new LocalFileSystem(baseFolder, new FakeProgressReporter());
        }

        private static IFileSystem Create(AutoTempFolder baseFolder)
        {
            return Create(baseFolder.Path);
        }
    }
}