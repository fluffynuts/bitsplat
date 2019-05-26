using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Storage;
using Castle.Core.Resource;
using static NExpect.Expectations;
using NExpect;
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
                [Ignore("WIP: need a stream piper")]
                public void ShouldCopyTheSourceFileToTarget()
                {
                    // Arrange
                    var basePath = Path.Combine(GetRandomArray<string>());
                    var relPath = "some-file.ext";
                    var data = GetRandomBytes();
                    var fs1 = CreateFileSystem(
                        basePath,
                        CreateFakeResource(
                            basePath,
                            relPath,
                            data)
                    );
                    var fs2 = CreateFileSystem(
                        GetRandomString());
                    var sut = Create();
                    // Act
                    sut.Synchronize(fs1, fs2);
                    // Assert
                    var inTarget = fs2.ListResourcesRecursive();
                    Expect(inTarget).To.Contain.Only(1).Item();
                    var copied = inTarget.Single();
                    Expect(copied.RelativePath).To.Equal(relPath);
                }
            }
        }

        private static IFileSystem CreateFileSystem(
            string basePath,
            params IFileResource[] resources)
        {
            var result = Substitute.For<IFileSystem>();
            var store = new List<IFileResource>(resources);
            store.ForEach(resource =>
            {
                var matcher = Arg.Is<string>(
                    a => a == resource.Path || a == resource.RelativePath);
                result.Exists(matcher)
                    .Returns(true);
                result.BasePath.Returns(basePath);

                result.IsDirectory(matcher)
                    .Returns(false);
                result.IsFile(matcher)
                    .Returns(true);
                result.IsFile(matcher)
                    .Returns(true);
            });

            result.Open(Arg.Any<string>(), Arg.Any<FileMode>())
                .Returns(ci =>
                {
                    var relativePath = ci.Arg<string>();
                    var mode = ci.Arg<FileMode>();

                    var match = store.FirstOrDefault(r => r.RelativePath == relativePath);
                    if (match == null)
                    {
                        // TODO: create new resource
                        var resource = CreateFakeResource(
                            basePath,
                            relativePath,
                            new byte[0]
                        );
                    }

                    switch (mode)
                    {
                        case FileMode.CreateNew:
                            throw new InvalidOperationException($"{relativePath} already exists");
                        case FileMode.Open:
                            return match.Read();
                        case FileMode.OpenOrCreate:
                        case FileMode.Create:
                            return match.Write();
                        case FileMode.Truncate:
                            var tstream = match.Write() as MemoryStream;
                            tstream.SetLength(0);
                            tstream.Rewind();
                            return tstream;
                        case FileMode.Append:
                            var astream = match.Write() as MemoryStream;
                            astream.Seek(0, SeekOrigin.End);
                            return astream;
                        default:
                            throw new InvalidOperationException($"Unknown file mode: {mode}");
                    }
                });

            result.ListResourcesRecursive()
                .Returns(resources);
            return result;
        }

        private static IFileResource CreateFakeResource(
            string basePath,
            string relativePath,
            byte[] data)
        {
            var result = Substitute.For<IFileResource>();
            result.Path.Returns(Path.Combine(basePath, relativePath));
            result.RelativePath.Returns(relativePath);
            MemoryStream memStream = null;

            result.Size.Returns(a => RetrieveData()
                .Length);
            result.Read()
                .Returns(a => GetStream());
            result.Write()
                .Returns(a => GetStream());
            return result;

            byte[] RetrieveData()
            {
                return GetStream().ToArray();
            }

            MemoryStream GetStream()
            {
                var stream = memStream ?? (memStream = CreateMemoryStreamContaining(data));
                memStream.Rewind();
                return stream;
            }
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
}