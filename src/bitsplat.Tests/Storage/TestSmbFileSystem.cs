using System;
using System.IO;
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
        private const string SMB_SHARE = "smb://192.168.50.105/mede8er";

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
            private static readonly string TestMountPath = $"/mnt/smb-test/{TEST_FOLDER}";
            private static readonly string TestSmbShare = $"{SMB_SHARE}/zzz_test";

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

            [SetUp]
            public void Setup()
            {
                EnsureTestFolderExists();
                Directory.Delete(TestMountPath, true);
                EnsureTestFolderExists();
            }

            [TearDown]
            public void TearDown()
            {
                Directory.Delete(TestMountPath);
            }

            private void EnsureTestFolderExists()
            {
                if (!Directory.Exists(TestMountPath))
                {
                    Directory.CreateDirectory(TestMountPath);
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
            return new SmbFileSystem(basePath ?? SMB_SHARE);
        }
    }
}