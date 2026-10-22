using System.IO;
using bitsplat.Archivers;
using bitsplat.Pipes;
using bitsplat.Storage;
using bitsplat.Tests.TestingSupport;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestMede8erArchiver
    {
        [TestFixture]
        public class WhenSourceAndTargetEmpty : TestMede8erArchiver
        {
            [Test]
            public void ShouldDoNothing()
            {
                // Arrange
                var source = Substitute.For<IFileSystem>();
                    source.ListResourcesRecursive()
                        .Returns(new IReadWriteFileResource[0]);
                var target = Substitute.For<IFileSystem>();
                target.ListResourcesRecursive()
                    .Returns(new IReadWriteFileResource[0]);
                var archive = Substitute.For<IFileSystem>();
                var sut = Create();
                // Act
                Expect(() => sut.RunArchiveOperations(
                        target, 
                        archive, 
                        source))
                    .Not.To.Throw();
                // Assert
                Expect(target)
                    .To.Have.Received(1)
                    .ListResourcesRecursive();
                Expect(source)
                    .To.Have.Received(1)
                    .ListResourcesRecursive();
                Expect(archive)
                    .Not.To.Have.Received()
                    .Open(Arg.Any<string>(), Arg.Any<FileMode>(), Arg.Any<FileAccess>());
            }
        }

        [TestFixture]
        public class WhenSourceHasNoFilesWithDotTExtension : TestMede8erArchiver
        {
            [Test]
            public void ShouldDoNothing()
            {
                // Arrange
                var sourceResource = Substitute.For<IReadWriteFileResource>();
                sourceResource.Path.Returns("some.file");
                var target = Substitute.For<IFileSystem>();
                target.ListResourcesRecursive()
                    .Returns(sourceResource.InArray());
                var source = Substitute.For<IFileSystem>();
                source.ListResourcesRecursive()
                    .Returns(sourceResource.InArray());
                var archive = Substitute.For<IFileSystem>();
                var sut = Create();
                // Act
                sut.RunArchiveOperations(target, archive, source);
                // Assert
                Expect(archive)
                    .Not.To.Have.Received()
                    .Open(Arg.Any<string>(), Arg.Any<FileMode>(), Arg.Any<FileAccess>());
                Expect(sourceResource)
                    .Not.To.Have.Received()
                    .OpenForRead();
            }
        }

        [TestFixture]
        public class WhenSourceHasSomeFilesReadyForArchive : TestMede8erArchiver
        {
            [Test]
            public void ShouldArchiveOnlyThoseFiles()
            {
                using var arena = new TestArena();
                // Arrange
                var toArchive = $"{GetRandomFileName()}";
                var archiveData = GetRandomBytes();
                var sourceFile = arena.CreateSourceFile(toArchive, archiveData);
                var targetFile = arena.CreateTargetFile(toArchive, archiveData);
                var targetMarker = arena.CreateTargetFile($"{sourceFile.Name}.t", new byte[0]);
                var archive = arena.ArchiveFileSystem;
                var target = arena.TargetFileSystem;
            var source = arena.SourceFileSystem;

                var sut = Create();
                // Act
                sut.RunArchiveOperations(
                    target,
                    archive,
                    source);
                // Assert
                // archive target should have archived file
                // -> should not be at target
                Expect(arena.TargetFileSystem.Exists(toArchive))
                    .To.Be.False("Target file should not exist any more");
                // -> should not be at source either
                Expect(arena.SourceFileSystem.Exists(toArchive))
                    .To.Be.False("Source file should not exist any more");
                // archive marker should no longer exist
                Expect(arena.TargetFileSystem.Exists($"{toArchive}.t"))
                    .To.Be.False("Target file marker should not exist any more");
                // resource file _should_ exist at archive
                Expect(arena.ArchiveFileSystem.Exists(toArchive))
                    .To.Be.True("Resource should be archived");
                // marker file _should not_ exist at archive
                Expect(arena.ArchiveFileSystem.Exists($"{toArchive}.t"))
                    .To.Be.False("Marker should not be present in archive");
            }
        }

        [TestFixture]
        public class WhenArchiveCopyFails : TestMede8erArchiver
        {
            [Test]
            public void ShouldKeepTheSourceFileTargetFileAndMarker()
            {
                // Arrange
                using var arena = new TestArena();
                var toArchive = GetRandomFileName();
                var data = GetRandomBytes();
                var sourceFile = arena.CreateSourceFile(toArchive, data);
                var targetFile = arena.CreateTargetFile(toArchive, data);
                var marker = arena.CreateTargetFile($"{toArchive}.t", new byte[0]);
                var archive = Substitute.For<IFileSystem>();
                archive.ListResourcesRecursive()
                    .Returns(new IReadWriteFileResource[0]);
                archive.Open(Arg.Any<string>(), Arg.Any<FileMode>(), Arg.Any<FileAccess>())
                    .Returns<Stream>(_ => throw new IOException("archive is full"));
                var sut = Create(options: CreateOptionsWithRetries(1));
                // Act
                sut.RunArchiveOperations(
                    arena.TargetFileSystem,
                    archive,
                    arena.SourceFileSystem);
                // Assert
                Expect(sourceFile.Path)
                    .To.Exist();
                Expect(targetFile.Path)
                    .To.Exist();
                Expect(marker.Path)
                    .To.Exist();
            }
        }

        [TestFixture]
        public class WhenArchiveDoesNotHaveTheFileAfterCopy : TestMede8erArchiver
        {
            [Test]
            public void ShouldKeepTheSourceFileTargetFileAndMarker()
            {
                // Arrange
                using var arena = new TestArena();
                var toArchive = GetRandomFileName();
                var data = GetRandomBytes();
                var sourceFile = arena.CreateSourceFile(toArchive, data);
                var targetFile = arena.CreateTargetFile(toArchive, data);
                var marker = arena.CreateTargetFile($"{toArchive}.t", new byte[0]);
                // accepts the write, but the file isn't there afterwards
                var archive = Substitute.For<IFileSystem>();
                archive.ListResourcesRecursive()
                    .Returns(new IReadWriteFileResource[0]);
                archive.Open(Arg.Any<string>(), Arg.Any<FileMode>(), Arg.Any<FileAccess>())
                    .Returns(_ => new MemoryStream());
                archive.FetchSize(Arg.Any<string>())
                    .Returns(-1);
                var messageWriter = Substitute.For<IMessageWriter>();
                var sut = Create(
                    options: CreateOptionsWithRetries(1),
                    messageWriter: messageWriter);
                // Act
                sut.RunArchiveOperations(
                    arena.TargetFileSystem,
                    archive,
                    arena.SourceFileSystem);
                // Assert
                Expect(sourceFile.Path)
                    .To.Exist();
                Expect(targetFile.Path)
                    .To.Exist();
                Expect(marker.Path)
                    .To.Exist();
                Expect(messageWriter)
                    .To.Have.Received(1)
                    .Write(Arg.Is<string>(s => s.Contains(toArchive)));
            }
        }

        [TestFixture]
        public class WhenMarkedFileIsNotAtSource : TestMede8erArchiver
        {
            [Test]
            public void ShouldKeepTheTargetFileAndMarkerAndReportIt()
            {
                // Arrange
                using var arena = new TestArena();
                var toArchive = GetRandomFileName();
                var targetFile = arena.CreateTargetFile(toArchive, GetRandomBytes());
                var marker = arena.CreateTargetFile($"{toArchive}.t", new byte[0]);
                var messageWriter = Substitute.For<IMessageWriter>();
                var sut = Create(
                    options: CreateOptionsWithRetries(1),
                    messageWriter: messageWriter);
                // Act
                sut.RunArchiveOperations(
                    arena.TargetFileSystem,
                    arena.ArchiveFileSystem,
                    arena.SourceFileSystem);
                // Assert
                Expect(targetFile.Path)
                    .To.Exist();
                Expect(marker.Path)
                    .To.Exist();
                Expect(arena.ArchiveFileSystem.Exists(toArchive))
                    .To.Be.False();
                Expect(messageWriter)
                    .To.Have.Received(1)
                    .Write(Arg.Is<string>(s => s.Contains(toArchive)));
            }
        }

        [TestFixture]
        public class WhenDryRun : TestMede8erArchiver
        {
            [Test]
            public void ShouldNotDeleteAnything()
            {
                // Arrange
                using var arena = new TestArena();
                var toArchive = GetRandomFileName();
                var data = GetRandomBytes();
                var sourceFile = arena.CreateSourceFile(toArchive, data);
                var targetFile = arena.CreateTargetFile(toArchive, data);
                var marker = arena.CreateTargetFile($"{toArchive}.t", new byte[0]);
                var options = CreateOptionsWithRetries(1);
                options.DryRun.Returns(true);
                var sut = Create(options: options);
                // Act
                sut.RunArchiveOperations(
                    arena.TargetFileSystem,
                    arena.ArchiveFileSystem,
                    arena.SourceFileSystem);
                // Assert
                Expect(sourceFile.Path)
                    .To.Exist();
                Expect(targetFile.Path)
                    .To.Exist();
                Expect(marker.Path)
                    .To.Exist();
            }
        }

        private static IOptions CreateOptionsWithRetries(int retries)
        {
            var options = Substitute.For<IOptions>();
            options.Retries.Returns(retries);
            return options;
        }

        private IArchiver Create(
            IProgressReporter progressReporter = null,
            IOptions options = null,
            IMessageWriter messageWriter = null)
        {
            return new Mede8erArchiver(
                new IPassThrough[0],
                progressReporter ?? new FakeProgressReporter(),
                options ?? Substitute.For<IOptions>(),
                messageWriter ?? Substitute.For<IMessageWriter>()
            );
        }
    }
}
