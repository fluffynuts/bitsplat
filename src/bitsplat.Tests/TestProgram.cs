using System.IO;
using bitsplat.History;
using bitsplat.Tests.TestingSupport;
using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;

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
            var expected = arena.TargetPathFor(newFile.RelativePath);
            // Act
            Program.Main(
                "-q",
                "-s", arena.SourcePath,
                "-t", arena.TargetPath
            );
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
            var expected = arena.TargetPathFor(newFile.RelativePath);
            // Act
            Program.Main(
                "-q",
                "-n",
                "-s", arena.SourcePath,
                "-t", arena.TargetPath
            );
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

            // Act
            Program.Main(
                "-s", arena.SourcePath,
                "-t", arena.TargetPath,
                "-a", arena.ArchivePath,
                "-q"
            );
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
            public void ShouldNotRecopyFileInHistory()
            {
                // Arrange
                using var arena = CreateArena();
                var sourceFile = arena.CreateSourceFile();
                var expectedTarget = Path.Combine(arena.TargetPath, sourceFile.RelativePath);

                // Act
                Program.Main(
                    "-q",
                    "-s", arena.SourcePath,
                    "-t", arena.TargetPath
                );
                Expect(expectedTarget)
                    .To.Exist();
                File.Delete(expectedTarget);
                Program.Main(
                    "-q",
                    "-s", arena.SourcePath,
                    "-t", arena.TargetPath
                );
                // Assert
                Expect(expectedTarget)
                    .Not.To.Exist();
            }

            [Test]
            public void ShouldNotRecopyFileInHistoryFromRoot()
            {
                // Arrange
                using var arena = CreateArena();
                var sourceFile = arena.CreateSourceFile("Movie.mkv");
                var expectedTarget = Path.Combine(arena.TargetPath, sourceFile.RelativePath);
                var expectedArchive = Path.Combine(arena.ArchivePath, sourceFile.RelativePath);

                // Act
                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s",
                        arena.SourcePath,
                        "-t",
                        arena.TargetPath,
                        "-a",
                        arena.ArchivePath
                    });
                Expect(expectedTarget)
                    .To.Exist();
                arena.CreateTargetFile("Movie.mkv.t");

                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s",
                        arena.SourcePath,
                        "-t",
                        arena.TargetPath,
                        "-a",
                        arena.ArchivePath
                    });
                Expect(expectedTarget)
                    .Not.To.Exist();
                Expect(expectedArchive)
                    .To.Exist();
                Expect(sourceFile.RelativePath)
                    .Not.To.Exist();

                // recreate the source
                arena.CreateSourceFile(sourceFile.RelativePath, sourceFile.Data);
                Program.Main(
                    "-q",
                    "-s", arena.SourcePath,
                    "-t", arena.TargetPath,
                    "-a", arena.ArchivePath
                );
                Expect(expectedTarget)
                    .Not.To.Exist();
                // Assert
            }
        }

        [TestFixture]
        public class WhenHistoryIsNotKept
        {
            [Test]
            public void ShouldRecopyMissingTargetFile()
            {
                // Arrange
                using var arena = CreateArena();
                var sourceFile = arena.CreateSourceFile();
                var expectedTarget = Path.Combine(arena.TargetPath, sourceFile.RelativePath);

                // Act
                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s", arena.SourcePath,
                        "-t", arena.TargetPath
                    });
                Expect(expectedTarget)
                    .To.Exist();
                File.Delete(expectedTarget);
                Program.Main(
                    "-q", "-s", arena.SourcePath, "-t", arena.TargetPath, "-n"
                );
                // Assert
                Expect(expectedTarget)
                    .To.Exist();
            }
        }

        [TestFixture]
        public class WhenRunningInOptInMode
        {
            [TestFixture]
            public class WhenNoHistory
            {
                [Test]
                public void ShouldOnlySyncCommonFoldersAndRoot()
                {
                    // Arrange
                    using var arena = CreateArena();
                    var rootFileName = GetRandomString(2);
                    var include = GetRandomString(2);
                    var exclude = GetAnother(include);

                    var seasonNumber = GetRandomInt(1, 9);
                    var season = $"Season 0{seasonNumber}";
                    var epi = $"S0{seasonNumber}E01 Epic Title.mkv";

                    var includeFile = arena.CreateSourceFile(
                        Path.Combine(include, season, epi)
                    );

                    arena.CreateSourceFile(
                        rootFileName
                    );

                    var excludeFile = arena.CreateSourceFile(
                        Path.Combine(exclude, season, epi),
                        includeFile.Data
                    );

                    arena.CreateTargetFolder(include);
                    Expect(arena.TargetPathFor(include))
                        .To.Exist();
                    var expected = arena.TargetPathFor(includeFile.RelativePath);
                    var unexpected = arena.TargetPathFor(excludeFile.RelativePath);

                    // Act
                    Program.Main(
                        "-m", "opt-in",
                        "-s", arena.SourcePath,
                        "-t", arena.TargetPath,
                        "-q"
                    );
                    // Assert
                    Expect(expected)
                        .To.Exist();
                    Expect(unexpected)
                        .Not.To.Exist();
                    Expect(arena.TargetPathFor(rootFileName))
                        .To.Exist();
                }
            }
        }

        private static TestArena CreateArena()
        {
            return new TestArena();
        }
    }
}