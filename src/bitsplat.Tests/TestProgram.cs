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
            public void ShouldNotRecopyFileInHistory()
            {
                // Arrange
                var arena = CreateArena();
                var sourceFile = arena.CreateSourceFile();
                var expectedTarget = Path.Combine(arena.TargetPath, sourceFile.RelativePath);

                // Act
                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s",
                        arena.SourcePath,
                        "-t",
                        arena.TargetPath
                    });
                Expect(expectedTarget)
                    .To.Exist();
                File.Delete(expectedTarget);
                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s",
                        arena.SourcePath,
                        "-t",
                        arena.TargetPath
                    });
                // Assert
                Expect(expectedTarget)
                    .Not.To.Exist();
            }
            
            [Test]
            public void ShouldNotRecopyFileInHistoryFromRoot()
            {
                // Arrange
                var arena = CreateArena();
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
                var arena = CreateArena();
                var sourceFile = arena.CreateSourceFile();
                var expectedTarget = Path.Combine(arena.TargetPath, sourceFile.RelativePath);

                // Act
                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s",
                        arena.SourcePath,
                        "-t",
                        arena.TargetPath
                    });
                Expect(expectedTarget)
                    .To.Exist();
                File.Delete(expectedTarget);
                Program.Main(
                    new[]
                    {
                        "-q",
                        "-s",
                        arena.SourcePath,
                        "-t",
                        arena.TargetPath,
                        "-n"
                    });
                // Assert
                Expect(expectedTarget)
                    .To.Exist();
            }
        }

        private static TestArena CreateArena()
        {
            return new TestArena();
        }
    }
}