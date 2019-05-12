using System;
using System.IO;
using NUnit.Framework;
using static NExpect.Expectations;
using NExpect;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class FileSystem
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
            using (var folder = new AutoTempFolder())
            {
                var baseFolder = Path.Combine(folder.Path, Guid.NewGuid().ToString());
                Expect(baseFolder).Not.To.Exist();
                // Act
                Expect(() => Create(baseFolder))
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
                    Expect(result)
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
                    Expect(result).To.Be.False();
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
                    Expect(sub).To.Be.A.Directory();
                    var sut = Create(tempDir.Path);
                    // Act
                    var result = sut.IsDirectory(test);
                    // Assert
                    Expect(result).To.Be.True();
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
                    Expect(sub).Not.To.Exist();
                    var sut = Create(tempDir.Path);
                    // Act
                    var result = sut.IsDirectory(test);
                    // Assert
                    Expect(result).To.Be.False();
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
                    Expect(Path.Combine(tempFolder.Path, fileName)).Not.To.Exist();
                    var expected = GetRandomBytes(1024);
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
                    Expect(written).To.Equal(expected);
                }
            }            
            
            [Test]
            public void WhenFileDoesNotExist_AndModeIs_Append_ShouldCreateFile()
            {
                // Arrange
                using (var tempFolder = new AutoTempFolder())
                {
                    var fileName = Guid.NewGuid().ToString();
                    Expect(Path.Combine(tempFolder.Path, fileName)).Not.To.Exist();
                    var expected = GetRandomBytes(1024);
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
                    Expect(written).To.Equal(expected);
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

    public static class Matchers
    {
        public static void Exist(
            this ITo<string> to)
        {
            to.AddMatcher(actual =>
            {
                var passed = System.IO.File.Exists(actual) || System.IO.Directory.Exists(actual);
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual} {passed.AsNot()}to exist");
            });
        }
        public static void Exist(
            this IStringToAfterNot to)
        {
            to.AddMatcher(actual =>
            {
                var passed = System.IO.File.Exists(actual) || System.IO.Directory.Exists(actual);
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual} {passed.AsNot()}to exist");
            });
        }

        public static void Directory(this IA<string> a)
        {
            a.AddMatcher(actual =>
            {
                var passed = System.IO.Directory.Exists(actual);
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual} {passed.AsNot()}to exist");
            });
        }
        
        public static void File(this IA<string> a)
        {
            a.AddMatcher(actual =>
            {
                var passed = System.IO.File.Exists(actual);
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual} {passed.AsNot()}to exist");
            });
        }
    }
}