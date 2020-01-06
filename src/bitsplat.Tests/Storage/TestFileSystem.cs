using System;
using bitsplat.Pipes;
using bitsplat.Storage;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.Utils;
using static NExpect.Expectations;

namespace bitsplat.Tests.Storage
{
    [TestFixture]
    public class TestFileSystemFactory
    {
        [TestFixture]
        public class FileSystemFor
        {
            [Test]
            public void ShouldProvideLocalFileSystemForLocalPath()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                var sut = Create();
                // Act
                var result = sut.FileSystemFor(folder.Path);
                // Assert
                Expect(result)
                    .To.Be.An.Instance.Of<LocalFileSystem>();
                Expect(result.BasePath)
                    .To.Equal(folder.Path);
            }

            [Test]
            public void ShouldProvideLocalFileSystemForFileProtocol()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                var uri = new Uri(folder.Path);
                var sut = Create();
                // Act
                var result = sut.FileSystemFor(uri.AbsoluteUri);
                // Assert
                Expect(result)
                    .To.Be.An.Instance.Of<LocalFileSystem>();
                Expect(result.BasePath)
                    .To.Equal(folder.Path);
            }

            [Test]
            public void ShouldNotYetKnowHowToProvideFtpFileSystem()
            {
                // Arrange
                var uri = "ftp://some.server/some/path";
                var sut = Create();
                // Act
                Expect(() => sut.FileSystemFor(uri))
                    .To.Throw<NotSupportedException>()
                    .With.Message.Containing("Protocol not supported: ftp");
                // Assert
            }
        }

        [TestFixture]
        public class CachingFileSystemFor
        {
            [Test]
            public void ShouldProvideLocalFileSystemForLocalPath()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                var sut = Create();
                // Act
                var result = sut.CachingFileSystemFor(folder.Path);
                // Assert
                Expect(result)
                    .To.Be.An.Instance.Of<CachingFileSystem>();
                Expect(result.BasePath)
                    .To.Equal(folder.Path);
            }

            [Test]
            public void ShouldProvideLocalFileSystemForFileProtocol()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                var uri = new Uri(folder.Path);
                var sut = Create();
                // Act
                var result = sut.CachingFileSystemFor(uri.AbsoluteUri);
                // Assert
                Expect(result)
                    .To.Be.An.Instance.Of<CachingFileSystem>();
                Expect(result.BasePath)
                    .To.Equal(folder.Path);
            }

            [Test]
            public void ShouldNotYetKnowHowToProvideFtpFileSystem()
            {
                // Arrange
                var uri = "ftp://some.server/some/path";
                var sut = Create();
                // Act
                Expect(() => sut.CachingFileSystemFor(uri))
                    .To.Throw<NotSupportedException>()
                    .With.Message.Containing("Protocol not supported: ftp");
                // Assert
            }
        }

        private static IFileSystemFactory Create(
            IMessageWriter messageWriter = null)
        {
            return new FileSystemFactory(
                messageWriter ?? Substitute.For<IMessageWriter>()
            );
        }
    }
}