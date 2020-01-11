using System.IO;
using bitsplat.History;
using bitsplat.Tests.TestingSupport;
using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestProgram
    {
        [Test]
        public void ShouldSyncOneFileInRoot()
        {
            // Arrange
            using var arena = CreateArena();
            var newFile = arena.CreateSourceFile();
            Expect(newFile.Path)
                .To.Exist();
            var args = new[]
            {
                "-q",
                "-s",
                arena.SourcePath,
                "-t",
                arena.TargetPath
            };
            var expected = arena.TargetPathFor(newFile.RelativePath);
            // Act
            Program.Main(args);
            // Assert
            Expect(expected)
                .To.Exist();
            var data = File.ReadAllBytes(expected);
            Expect(data)
                .To.Equal(newFile.Data);
            var dbFile = Path.Combine(arena.TargetPath, TargetHistoryRepository.DB_NAME);
            Expect(dbFile)
                .To.Exist();
        }

        [Test]
        public void ShouldNotCreateTargetHistoryIfNoHistorySpecified()
        {
            // Arrange
            using var arena = CreateArena();
            var newFile = arena.CreateSourceFile();
            Expect(newFile.Path)
                .To.Exist();
            var args = new[]
            {
                "-q",
                "-n",
                "-s",
                arena.SourcePath,
                "-t",
                arena.TargetPath
            };
            var expected = arena.TargetPathFor(newFile.RelativePath);
            // Act
            Program.Main(args);
            // Assert
            Expect(expected)
                .To.Exist();
            var data = File.ReadAllBytes(expected);
            Expect(data)
                .To.Equal(newFile.Data);
            var dbFile = Path.Combine(arena.TargetPath, TargetHistoryRepository.DB_NAME);
            Expect(dbFile)
                .Not.To.Exist();
        }

        [Test]
        public void ShouldArchiveAWatchedFile()
        {
            // Arrange
            using var arena = CreateArena();
            var series = "Some Series";
            var season = "Season 01";
            var episode1 = "Episode 1.mkv";
            var episode2 = "Episode 2.mkv";

            var watchedMarker = arena.CreateTargetFile(
                Path.Combine(series, season, $"{episode1}.t")
            );
            var watchedFile = arena.CreateTargetFile(
                Path.Combine(series, season, episode1)
            );
            // it's expected that the file should still exist at the source
            arena.CreateSourceFile(
                Path.Combine(series, season, episode1)
            );
            arena.CreateSourceFile(
                Path.Combine(series, season, episode2)
            );

            var expectedArchivePath = arena.ArchivePathFor(
                series,
                season,
                episode1
            );
            var expectedTargetPath = arena.TargetPathFor(
                series,
                season,
                episode2
            );

            var args = new[]
            {
                "-s",
                arena.SourcePath,
                "-t",
                arena.TargetPath,
                "-a",
                arena.ArchivePath,
                "-q"
            };
            // Act
            Program.Main(args);
            // Assert
            Expect(watchedMarker.Path)
                .Not.To.Exist();
            Expect(watchedFile.Path)
                .Not.To.Exist();
            Expect(expectedArchivePath)
                .To.Exist();
            Expect(expectedTargetPath)
                .To.Exist();
        }

        [TestFixture]
        public class WhenHistoryIsKept
        {
            [Test]
            [Explicit("WIP")]
            public void ShouldNotRecopyFileInHistory()
            {
                // Arrange

                // Act
                // Assert
            }
        }

        [TestFixture]
        public class WhenHistoryIsNotKept
        {
            [Test]
            [Explicit("WIP")]
            public void ShouldRecopyMissingTargetFile()
            {
                // Arrange

                // Act
                // Assert
            }
        }

        private static TestArena CreateArena()
        {
            return new TestArena();
        }
    }
}