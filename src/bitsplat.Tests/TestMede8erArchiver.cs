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
                    .Open(Arg.Any<string>(), Arg.Any<FileMode>());
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
                    .Returns(sourceResource.AsArray());
                var source = Substitute.For<IFileSystem>();
                source.ListResourcesRecursive()
                    .Returns(sourceResource.AsArray());
                var archive = Substitute.For<IFileSystem>();
                var sut = Create();
                // Act
                sut.RunArchiveOperations(target, archive, source);
                // Assert
                Expect(archive)
                    .Not.To.Have.Received()
                    .Open(Arg.Any<string>(), Arg.Any<FileMode>());
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

        private IArchiver Create(
            IProgressReporter progressReporter = null)
        {
            return new Mede8erArchiver(
                new IPassThrough[0],
                progressReporter ?? new FakeProgressReporter()
            );
        }
    }
}