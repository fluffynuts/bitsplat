using System;
using System.IO;
using NExpect;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestFileSystem
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
                var baseFolder = Path.Combine(folder.Path, Guid.NewGuid().ToString());
                Expectations.Expect(baseFolder).Not.To.Exist();
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
                    var test = Path.Combine(tempFolder.Path, Guid.NewGuid().ToString());
                    var sut = Create(test);
                    // Act
                    var result = sut.IsFile(test);
                    // Assert
                    Expectations.Expect(result).To.Be.False();
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
                    var test = Guid.NewGuid().ToString();
                    var sub = Path.Combine(tempDir.Path, test);
                    Directory.CreateDirectory(sub);
                    Expectations.Expect(sub).To.Be.A.Directory();
                    var sut = Create(tempDir.Path);
                    // Act
                    var result = sut.IsDirectory(test);
                    // Assert
                    Expectations.Expect(result).To.Be.True();
                }
            }            
            
            [Test]
            public void WhenDirectoryDoesNotExist_RelativeToCtorPath_ShouldReturnFalse()
            {
                // Arrange
                using (var tempDir = new AutoTempFolder())
                {
                    var test = Guid.NewGuid().ToString();
                    var sub = Path.Combine(tempDir.Path, test);
                    Expectations.Expect(sub).Not.To.Exist();
                    var sut = Create(tempDir.Path);
                    // Act
                    var result = sut.IsDirectory(test);
                    // Assert
                    Expectations.Expect(result).To.Be.False();
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
                    Expectations.Expect(result).To.Be.True();
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
                    Expectations.Expect(result).To.Be.True();
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
                    var result = sut.Exists(Guid.NewGuid().ToString());
                    // Assert
                    Expectations.Expect(result).To.Be.False();
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
                    var fileName = Guid.NewGuid().ToString();
                    Expectations.Expect(Path.Combine(tempFolder.Path, fileName)).Not.To.Exist();
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
                    Expectations.Expect(written).To.Equal(expected);
                }
            }            
            
            [Test]
            public void WhenFileDoesNotExist_AndModeIs_Append_ShouldCreateFile()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var fileName = Guid.NewGuid().ToString();
                    Expectations.Expect(Path.Combine(tempFolder.Path, fileName)).Not.To.Exist();
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
                    Expectations.Expect(written).To.Equal(expected);
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