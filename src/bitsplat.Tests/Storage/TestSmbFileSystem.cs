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
        public class ListResourcesRecursive
        {
            private const string TEST_FOLDER = "zzz_test";
            private static readonly string TestSmbShare = $"{SmbShareUrl}/zzz_test";

            private static readonly string TestFolderPath = 
                Platform.IsUnixy
                    ? $"/mnt/smb-test/{TEST_FOLDER}"
                    : $"\\\\{SMB_SERVER_IP}\\{SMB_SHARE_NAME}\\{TEST_FOLDER}";

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
        }

        private static IFileSystem Create(
            string basePath = null
        )
        {
            return new SmbFileSystem(basePath ?? SmbShareUrl);
        }
    }
}