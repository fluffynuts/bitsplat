using System;
using System.IO;
using System.Linq;
using bitsplat.Storage;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using PeanutButter.Utils;
using SharpCifs.Smb;
using static NExpect.Expectations;

namespace bitsplat.Tests.Storage
{
    [TestFixture]
    [Explicit("Requires known local samba server")]
    public class TestSmbFileSystem
    {
        private const string SMB_SERVER_IP = "192.168.50.105";
        private const string SMB_SHARE_NAME = "mede8er";
        private static readonly string SmbShareUrl = $"smb://{SMB_SERVER_IP}/{SMB_SHARE_NAME}";

        [TestFixture]
        public class Exists
        {
            [Test]
            public void ShouldReturnTrueForExistingFile()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.Exists("View.xml");
                // Assert
                Expect(result)
                    .To.Be.True();
            }

            [Test]
            public void ShouldReturnFalseForFileNotFound()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.Exists("View.xml.not-found");
                // Assert
                Expect(result)
                    .To.Be.False();
            }

            [Test]
            public void ShouldReturnTrueForExistingFolder()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.Exists("series");
                // Assert
                Expect(result)
                    .To.Be.True();
            }
        }

        [TestFixture]
        public class IsFile
        {
            [Test]
            public void ShouldReturnTrueForExistingFile()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.IsFile("View.xml");
                // Assert
                Expect(result)
                    .To.Be.True();
            }

            [Test]
            public void ShouldReturnFalseForFileNotFound()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.IsFile("View.xml.not-found");
                // Assert
                Expect(result)
                    .To.Be.False();
            }

            [Test]
            public void ShouldReturnFalseForExistingFolder()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.IsFile("series");
                // Assert
                Expect(result)
                    .To.Be.False();
            }
        }

        [TestFixture]
        public class IsDirectory
        {
            [Test]
            public void ShouldReturnFalseForExistingFile()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.IsDirectory("View.xml");
                // Assert
                Expect(result)
                    .To.Be.False();
            }

            [Test]
            public void ShouldReturnFalseForFileNotFound()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.IsDirectory("View.xml.not-found");
                // Assert
                Expect(result)
                    .To.Be.False();
            }

            [Test]
            public void ShouldReturnTrueForExistingFolder()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.IsDirectory("series");
                // Assert
                Expect(result)
                    .To.Be.True();
            }
        }

        [TestFixture]
        public class TestsRequiringIO
        {
            [TestFixture]
            public class ListResourcesRecursive: TestsRequiringIO
            {

                [Test]
                public void ShouldReturnEmptyCollectionWhenEmpty()
                {
                    // Arrange
                    var sut = Create(TestSmbShare);
                    // Act
                    var result = sut.ListResourcesRecursive();
                    // Assert
                    Expect(result)
                        .To.Be.Empty();
                }

                [Test]
                public void ShouldReturnSingleFileInRootOfShare()
                {
                    // Arrange
                    var data = GetRandomWords();
                    var filename = GetRandomString(4);
                    var filePath = Path.Combine(TestFolderPath, filename);
                    File.WriteAllText(filePath, data);
                    var sut = Create(TestSmbShare);
                    // Act
                    var result = sut.ListResourcesRecursive();
                    // Assert
                    Expect(result)
                        .To.Contain.Only(1)
                        .Matched.By(o => o.Name == filename);
                }

                [Test]
                public void ShouldReturnSingleFileInSubFolder()
                {
                    // Arrange
                    var data = GetRandomWords();
                    var filename = GetRandomString(4);
                    var subfolder = GetRandomString(4);
                    var fullPathToSubFolder = Path.Combine(TestFolderPath, subfolder);
                    var fullPath = Path.Combine(fullPathToSubFolder, filename);
                    Directory.CreateDirectory(fullPathToSubFolder);
                    File.WriteAllText(fullPath, data);
                    var sut = Create(TestSmbShare);
                    // Act
                    var result = sut.ListResourcesRecursive().ToArray();
                    // Assert
                    Expect(result)
                        .To.Contain.Only(1)
                        .Matched.By(o => o.Name == filename);
                    Expect(result[0].RelativePath)
                        .To.Equal($"{subfolder}/{filename}");
                }

                [Test]
                public void ShouldReturnAllFilesInTree()
                {
                    // Arrange
                    var filename1 = GetRandomString(4);
                    var filename2 = GetRandomString(4);
                    var filename3 = GetRandomString(4);
                    var subfolder1 = GetRandomString(4);
                    var subfolder2 = GetRandomString(4);
                    var filepath1 = Path.Combine(TestFolderPath, filename1);
                    var filepath2 = Path.Combine(TestFolderPath, subfolder1, filename2);
                    var filepath3 = Path.Combine(TestFolderPath, subfolder1, subfolder2, filename3);
                    Directory.CreateDirectory(Path.Combine(TestFolderPath, subfolder1));
                    Directory.CreateDirectory(Path.Combine(TestFolderPath, subfolder1, subfolder2));
                    File.WriteAllText(filepath1, GetRandomWords());
                    File.WriteAllText(filepath2, GetRandomWords());
                    File.WriteAllText(filepath3, GetRandomWords());
                    var sut = Create(TestSmbShare);
                    // Act
                    var result = sut.ListResourcesRecursive().ToArray();
                    // Assert
                    Expect(result)
                        .To.Contain.Only(3).Items();
                    Expect(result)
                        .To.Contain.Exactly(1)
                        .Matched.By(o => o.RelativePath == filename1);
                    Expect(result)
                        .To.Contain.Exactly(1)
                        .Matched.By(o => o.RelativePath == $"{subfolder1}/{filename2}");
                    Expect(result)
                        .To.Contain.Exactly(1)
                        .Matched.By(o => o.RelativePath == $"{subfolder1}/{subfolder2}/{filename3}");
                }

            }

            [TestFixture]
            public class ReadingFiles: TestsRequiringIO
            {
                [Test]
                [Explicit("WIP")]
                public void ShouldBeAbleToReadExistingFile()
                {
                    // Arrange
                    var filename = GetRandomString(4);
                    var data = GetRandomWords();
                    var filepath = Path.Combine(TestFolderPath, filename);
                    File.WriteAllText(filepath, data);
                    var sut = Create(TestSmbShare);
                    // Act
                    using var fs = sut.Open(filename, FileMode.Open, FileAccess.Read);
                    var read = fs.ReadAllBytes().AsString();
                    // Assert
                    Expect(read)
                        .To.Equal(data);
                }
            }

            private const string TEST_FOLDER = "zzz_test";
            private static readonly string TestSmbShare = $"{SmbShareUrl}/zzz_test";

            private static readonly string TestFolderPath = 
                Platform.IsUnixy
                    ? $"/mnt/smb-test/{TEST_FOLDER}"
                    : $"\\\\{SMB_SERVER_IP}\\{SMB_SHARE_NAME}\\{TEST_FOLDER}";
            
            [SetUp]
            public void Setup()
            {
                EnsureTestFolderExists();
                Directory.Delete(TestFolderPath, true);
                EnsureTestFolderExists();
            }

            [TearDown]
            public void TearDown()
            {
                Directory.Delete(TestFolderPath, recursive: true);
            }

            private void EnsureTestFolderExists()
            {
                if (!Directory.Exists(TestFolderPath))
                {
                    Directory.CreateDirectory(TestFolderPath);
                }
            }
        }

        [TestFixture]
        [Explicit("Testing capabilities of underlying CIFS library directly")]
        public class CifsLibTests
        {
            [Test]
            public void EnumeratingFiles()
            {
                // Arrange
                var smb = new SmbFile("smb://192.168.50.105/mede8er/series/");
                // Act
                foreach (var entry in smb.List())
                {
                    Console.WriteLine(entry);
                }
                // Assert
            }

            [Test]
            public void RewritingPartialFile()
            {
                // Arrange
                var file = new SmbFile("smb://192.168.50.105/mede8er/__resume-test__.txt");
                if (file.IsFile())
                {
                    file.Delete();
                }

                using (var out1 = new SmbFileOutputStream(file))
                {
                    out1.Write("hello, world!".AsBytes());
                }

                using (var out2 = new SmbFileOutputStream(file, true))
                {
                    var newstuff = "people!".AsBytes();
                    out2.SetPosition(out2.GetPosition() -  6);
                    out2.Write(newstuff);
                }

                // Act
                // Assert
            }

            [Test]
            public void RandomIOOnFile()
            {
                // Arrange
                // var shareUrl = "smb://192.168.50.105/mede8er";
                var shareUrl = "smb://localhost/smb-test";
                var file = new SmbFile($"{shareUrl}/__resume-test__.txt");
                if (file.IsFile())
                {
                    file.Delete();
                }

                using (var out1 = new SmbFileOutputStream(file))
                {
                    out1.Write("hello, world!".AsBytes());
                }

                var fs = new SmbFileSystem(shareUrl);
                using var s = new SmbFileStream(file, fs, false);
                // s.Seek(6, SeekOrigin.Begin);
                var buffer = "moo cows and some stuff".AsBytes();
                s.Write(buffer, 0, buffer.Length);
                // Act
                // Assert
            }
        }

        private static IFileSystem Create(
            string basePath = null
        )
        {
            return new SmbFileSystem(basePath ?? SmbShareUrl);
        }
    }

    [TestFixture]
    public class TestSmbFileStream
    {
    }
}