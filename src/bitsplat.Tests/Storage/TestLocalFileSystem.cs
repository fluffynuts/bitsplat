using System;
using System.IO;
using System.Linq;
using bitsplat.Storage;
using bitsplat.Tests.TestingSupport;
using NExpect;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

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
            Expectations.Expect(typeof(LocalFileSystem))
                .To.Implement<IFileSystem>();
            // Assert
        }

        [Test]
        public void Ctor_WhenBasePathDoesNotExist_ShouldAttemptToCreateIt()
        {
            // Arrange
            using (var folder = new AutoTempFolder())
            {
                var baseFolder = Path.Combine(folder.Path,
                    Guid.NewGuid()
                        .ToString());
                Expectations.Expect(baseFolder)
                    .Not.To.Exist();
                // Act
                Expectations.Expect(() => Create(baseFolder))
                    .To.Throw<DirectoryNotFoundException>();
                // Assert
            }
        }

        [TestFixture]
        public class IsFile
        {
            [Test]
            public void WhenFileExists_RelativeToConstructionPath_ShouldReturnTrue()
            {
                // Arrange
                using (var tempFile = new AutoTempFile())
                {
                    var container = Path.GetDirectoryName(tempFile.Path);
                    var sut = Create(container);
                    // Act
                    var result = sut.IsFile(tempFile.Path);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.True();
                }
            }

            [Test]
            public void WhenFileDoesNotExist_RelativeToCtorPath_ShouldReturnFalse()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var test = Path.Combine(tempFolder.Path,
                        Guid.NewGuid()
                            .ToString());
                    var sut = Create(tempFolder);
                    // Act
                    var result = sut.IsFile(test);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.False();
                }
            }
        }

        [TestFixture]
        public class IsDirectory
        {
            [Test]
            public void WhenDirectoryExists_RelativeToCtorPath_ShouldReturnTrue()
            {
                // Arrange
                using (var tempDir = new AutoTempFolder())
                {
                    var test = Guid.NewGuid()
                        .ToString();
                    var sub = Path.Combine(tempDir.Path, test);
                    Directory.CreateDirectory(sub);
                    Expectations.Expect(sub)
                        .To.Be.A.Directory();
                    var sut = Create(tempDir.Path);
                    // Act
                    var result = sut.IsDirectory(test);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.True();
                }
            }

            [Test]
            public void WhenDirectoryDoesNotExist_RelativeToCtorPath_ShouldReturnFalse()
            {
                // Arrange
                using (var tempDir = new AutoTempFolder())
                {
                    var test = Guid.NewGuid()
                        .ToString();
                    var sub = Path.Combine(tempDir.Path, test);
                    Expectations.Expect(sub)
                        .Not.To.Exist();
                    var sut = Create(tempDir.Path);
                    // Act
                    var result = sut.IsDirectory(test);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.False();
                }
            }
        }

        [TestFixture]
        public class Exists
        {
            [Test]
            public void WhenFileExists_ShouldReturnTrue()
            {
                // Arrange
                using (var tempFile = new AutoTempFile())
                {
                    var sut = Create(Path.GetDirectoryName(tempFile.Path));
                    // Act
                    var result = sut.Exists(tempFile.Path);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.True();
                }
            }

            [Test]
            public void WhenDirectoryExists_ShouldReturnTrue()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var sut = Create(tempFolder.Path);
                    // Act
                    var result = sut.Exists(tempFolder.Path);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.True();
                }
            }

            [Test]
            public void WhenNothingFound_ShouldReturnFalse()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var sut = Create(tempFolder);
                    // Act
                    var result = sut.Exists(Guid.NewGuid()
                        .ToString());
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.False();
                }
            }
        }

        [TestFixture]
        public class Open
        {
            [Test]
            public void WhenFileDoesNotExist_AndModeIs_OpenOrCreate_ShouldCreateFile()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var fileName = Guid.NewGuid()
                        .ToString();
                    Expectations.Expect(Path.Combine(tempFolder.Path, fileName))
                        .Not.To.Exist();
                    var expected = RandomValueGen.GetRandomBytes(1024);
                    var sut = Create(tempFolder);
                    // Act
                    using (var stream = sut.Open(fileName, FileMode.OpenOrCreate))
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
                    Expectations.Expect(written)
                        .To.Equal(expected);
                }
            }

            [Test]
            public void WhenFileDoesNotExist_AndModeIs_Append_ShouldCreateFile()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var fileName = Guid.NewGuid()
                        .ToString();
                    Expectations.Expect(Path.Combine(tempFolder.Path, fileName))
                        .Not.To.Exist();
                    var expected = RandomValueGen.GetRandomBytes(1024);
                    var sut = Create(tempFolder);
                    // Act
                    using (var stream = sut.Open(fileName, FileMode.Append))
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
                    Expectations.Expect(written)
                        .To.Equal(expected);
                }
            }
        }

        [TestFixture]
        public class ListRecursive
        {
            [Test]
            public void ShouldReturnEmptyCollectionForEmptyBase()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var sut = Create(tempFolder);
                    // Act
                    var results = sut.ListResourcesRecursive();
                    // Assert
                    Expectations.Expect(results)
                        .Not.To.Be.Null();
                    Expectations.Expect(results)
                        .To.Be.Empty();
                }
            }

            [Test]
            public void ShouldReturnSingleFileUnderBasePath()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var filePath = tempFolder.CreateRandomFile();
                    var sut = Create(tempFolder);
                    // Act
                    var results = sut.ListResourcesRecursive();
                    // Assert
                    Expectations.Expect(results)
                        .To.Contain.Exactly(1)
                        .Matched.By(r => r.Path == filePath);
                }
            }

            [Test]
            public void FirstLevelResultsShouldHaveCorrectRelativePath()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var path = tempFolder.CreateRandomFile();
                    var expected = Path.GetFileName(path);
                    var sut = Create(tempFolder);
                    // Act
                    var results = sut.ListResourcesRecursive();
                    // Assert
                    Expectations.Expect(results)
                        .To.Contain.Exactly(1)
                        .Matched.By(r => r.RelativePath == expected);
                }
            }

            [Test]
            public void ShouldIgnoreEmptyFoldersUnderBase()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var path = tempFolder.CreateRandomFolder();
                    var sut = Create(tempFolder);
                    Expectations.Expect(path)
                        .To.Exist();
                    // Act
                    var results = sut.ListResourcesRecursive();
                    // Assert
                    Expectations.Expect(results)
                        .To.Be.Empty();
                }
            }

            [Test]
            public void ShouldListResourcesUnderFolders()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var file1 = tempFolder.CreateRandomFile();
                    var sub1 = tempFolder.CreateRandomFolder();
                    var file2 = Path.Combine(sub1, RandomValueGen.CreateRandomFileIn(sub1));
                    var file3 = Path.Combine(sub1, RandomValueGen.CreateRandomFileIn(sub1));
                    var sub2 = Path.Combine(sub1, RandomValueGen.CreateRandomFolderIn(sub1));
                    var file4 = Path.Combine(sub2, RandomValueGen.CreateRandomFileIn(sub2));

                    new[]
                    {
                        file1,
                        sub1,
                        file2,
                        file3,
                        sub2,
                        file4
                    }.ForEach(o => Expectations.Expect(o)
                        .To.Exist());
                    var sut = Create(tempFolder);
                    // Act
                    var result = sut.ListResourcesRecursive();
                    // Assert
                    Expectations.Expect(result).To.Contain.Only(4).Items();
                    Expectations.Expect(result).To.Contain.Exactly(1)
                        .Matched.By(o => o.Path == file1);
                    Expectations.Expect(result).To.Contain.Exactly(1)
                        .Matched.By(o => o.Path == file2);
                    Expectations.Expect(result).To.Contain.Exactly(1)
                        .Matched.By(o => o.Path == file3);
                    Expectations.Expect(result).To.Contain.Exactly(1)
                        .Matched.By(o => o.Path == file4);
                }
            }

            [Test]
            public void ShouldSupplyRelativePathOnDemand()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var file1 = tempFolder.CreateRandomFile();
                    var sub = tempFolder.CreateRandomFolder();
                    var file2 = Path.Combine(sub, RandomValueGen.CreateRandomFileIn(sub));
                    var expected1 = Path.GetRelativePath(tempFolder.Path, file1);
                    var expected2 = Path.GetRelativePath(tempFolder.Path, file2);
                    var sut = Create(tempFolder);
                    // Act
                    var results = sut.ListResourcesRecursive();
                    // Assert
                    Expectations.Expect(results).To.Contain.Only(2).Items();
                    Expectations.Expect(results).To.Contain.Exactly(1)
                        .Matched.By(o => o.RelativePath == expected1 &&
                                         o.Path == file1);
                    Expectations.Expect(results).To.Contain.Exactly(1)
                        .Matched.By(o => o.RelativePath == expected2 &&
                                         o.Path == file2);
                }
            }

            [Test]
            public void ShouldSupplySizeOnDemand()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var file = tempFolder.CreateRandomFile();
                    var stat = new FileInfo(file);
                    var newData = RandomValueGen.GetRandomBytes((int)stat.Length + 1, (int)stat.Length + 100);
                    var sut = Create(tempFolder);
                    // Act
                    var results = sut.ListResourcesRecursive();
                    File.WriteAllBytes(file, newData);
                    // Assert
                    Expectations.Expect(results.Single().Size)
                        .To.Equal(newData.Length);
                }
            }
            
            [Test]
            public void ShouldSupplyMinusOneForSizeIfUnableToStat()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var file = tempFolder.CreateRandomFile();
                    var stat = new FileInfo(file);
                    var sut = Create(tempFolder);
                    // Act
                    var results = sut.ListResourcesRecursive();
                    File.Delete(file);
                    // Assert
                    Expectations.Expect(results.Single().Size)
                        .To.Equal(-1);
                }
            }
        }

        private static IFileSystem Create(string baseFolder)
        {
            return new LocalFileSystem(baseFolder);
        }

        private static IFileSystem Create(AutoTempFolder baseFolder)
        {
            return Create(baseFolder.Path);
        }
    }
}