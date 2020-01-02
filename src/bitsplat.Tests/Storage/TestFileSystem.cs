using System;
using bitsplat.Storage;
using NExpect;
using NUnit.Framework;
using PeanutButter.Utils;
using static NExpect.Expectations;

namespace bitsplat.Tests.Storage
{
    [TestFixture]
    public class TestFileSystem
    {
        [Test]
        public void ShouldProvideLocalFileSystemForLocalPath()
        {
            // Arrange
            using var folder = new AutoTempFolder();
            // Act
            var result = FileSystem.For(folder.Path);
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
            // Act
            var result = FileSystem.For(uri.AbsoluteUri);
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
            // Act
            Expect(() => FileSystem.For(uri))
                .To.Throw<NotSupportedException>()
                .With.Message.Containing("Protocol not supported: ftp");
            // Assert
        }
    }
}