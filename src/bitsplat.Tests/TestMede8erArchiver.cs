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
                var sut = Create();
                // Act
                Expect(() => sut.RunArchiveOperations(source, target))
                    .Not.To.Throw();
                // Assert
                Expect(source)
                    .To.Have.Received(2)
                    .ListResourcesRecursive();
                Expect(target)
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
                var source = Substitute.For<IFileSystem>();
                source.ListResourcesRecursive()
                    .Returns(sourceResource.AsArray());
                var target = Substitute.For<IFileSystem>();
                var sut = Create();
                // Act
                sut.RunArchiveOperations(source, target);
                // Assert
                Expect(target)
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
                using (var arena = new TestArena())
                {
                    // Arrange
                    var toArchive = $"{GetRandomFileName()}";
                    var archiveData = GetRandomBytes();
                    var archiveFile = arena.CreateSourceFile(toArchive, archiveData);
                    var archiveMarker = arena.CreateSourceFile($"{archiveFile.Name}.t", new byte[0]);

                    var keepData = GetRandomBytes();
                    var toKeep = GetRandomFileName();
                    var keepFile = arena.CreateSourceFile(toKeep, keepData);
                    var source = arena.SourceFileSystem; //  new LocalFileSystem(arena.SourcePath);
                    var target = arena.TargetFileSystem; // new LocalFileSystem(arena.TargetPath);
                    var expectedFile = Path.Combine(arena.TargetPath, toArchive);

                    Expect(source.ListResourcesRecursive())
                        .To.Contain.Only(3)
                        .Matched.By(f =>
                            (f.Name == archiveMarker.Name && f.Size == 0) ||
                            (f.Name == toArchive && f.Size == archiveData.Length) ||
                            (f.Name == toKeep && f.Size == keepData.Length));
                    var sut = Create();
                    // Act
                    sut.RunArchiveOperations(
                        source,
                        target);
                    // Assert
                    // archive file and marker should have moved
                    Expect(archiveFile.Path)
                        .Not.To.Exist();
                    Expect(archiveMarker.Path)
                        .Not.To.Exist();

                    // keep file should still be there
                    Expect(keepFile.Path)
                        .To.Exist();
                    Expect(keepFile.Path)
                        .To.Have.Data(keepFile.Data);

                    // archive target should have archived file
                    Expect(expectedFile)
                        .To.Exist();
                    Expect(expectedFile)
                        .To.Have.Data(archiveData);
                    // archive marker should no longer exist
                    Expect($"{expectedFile}.t")
                        .Not.To.Exist();
                }
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