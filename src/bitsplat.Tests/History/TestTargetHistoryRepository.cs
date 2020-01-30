using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.Tests.TestingSupport;
using Dapper;
using NExpect;
using NExpect.Implementations;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.Utils;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;
using Table = bitsplat.Migrations.Constants.Tables.History;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable PossibleMultipleEnumeration

namespace bitsplat.Tests.History
{
    [TestFixture]
    public class TestTargetHistoryRepository
    {
        [Test]
        public void ShouldImplement_ITargetHistoryRepository()
        {
            // Arrange
            var sut = typeof(TargetHistoryRepository);
            // Act
            Expect(sut)
                .To.Implement<ITargetHistoryRepository>();
            // Assert
        }

        [TestFixture]
        public class Construction
        {
            [Test]
            public void WhenTargetDatabaseMissing_ShouldCreateIt()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                Expect(folder)
                    .Not.To.Have.Contents();
                // Act
                Create(folder);
                // Assert
                Expect(folder)
                    .To.Have.Contents();
                var files = Directory.GetFiles(folder.Path);
                Expect(files)
                    .To.Contain.Exactly(1)
                    .Item();
                var builder = new SQLiteConnectionStringBuilder()
                {
                    FullUri = new Uri(Path.Combine(folder.Path, files.First())).ToString()
                };
                var conn = new SQLiteConnection(builder.ToString());
                Expect(() => conn.Open())
                    .Not.To.Throw();
            }

            [Test]
            public void WhenTargetDatabaseMissing_ShouldAddHistoryTable()
            {
                // Arrange
                using var folder = new AutoTempFolder();
                Expect(folder)
                    .Not.To.Have.Contents();
                // Act
                Create(folder);
                // Assert
                Expect(folder)
                    .To.Have.Contents();
                var dataLayer = new SqLiteDatabase(folder.Path);
                using var conn = dataLayer.Connect();
                Expect(() =>
                        conn.Query<HistoryItem>($"select * from {Table.NAME};")
                    )
                    .Not.To.Throw();
            }

            [Test]
            public void ShouldMakeHistoryPathUnique()
            {
                // Arrange
                var path = GetRandomWindowsPath();
                var size1 = GetRandomInt();
                var size2 = GetRandomInt();
                using var folder = new AutoTempFolder();
                // Act
                Create(folder);
                // Assert
                using var conn = OpenBitsplatConnectionIn(folder);
                var sql = "insert into history (path, size) values (@path, @size)";
                conn.Execute(sql,
                    new
                    {
                        path,
                        size = size1
                    });
                Expect(() =>
                        conn.Execute(sql,
                            new
                            {
                                path,
                                size = size2
                            })
                    )
                    .To.Throw<SQLiteException>()
                    .With.Message.Containing("UNIQUE")
                    .Then("History.path");
            }
        }

        [TestFixture]
        public class Upsert
        {
            [Test]
            public void ShouldAddANewHistoryItem()
            {
                // Arrange
                var item = GetRandom<HistoryItem>();
                var beforeTest = DateTime.UtcNow.TruncateMilliseconds();
                using var arena = Create();
                // Act
                arena.SUT.Upsert(item);
                // Assert
                using var conn = arena.OpenConnection();
                var result = conn.Query<HistoryItem>($"select * from {Table.NAME};")
                    .ToArray();
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Item("Should have 1 result");
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(inDb => inDb.Path == item.Path &&
                                        inDb.Size == item.Size &&
                                        inDb.Created >= beforeTest,
                        () =>
                            $"Single result should match input\n{item.Stringify()}\nvs\n{result[0].Stringify()}\nbeforeTest:{beforeTest}");
            }

            [Test]
            public void ShouldUpdateSizeAndSetLastModifiedOnRepeatedPath()
            {
                // Arrange
                var item = GetRandom<HistoryItem>();
                var second = GetRandom<HistoryItem>();
                second.Path = item.Path;
                var beforeTest = DateTime.UtcNow.TruncateMilliseconds();
                using var arena = Create();
                // Act
                arena.SUT.Upsert(item);
                var initialDb = arena.SUT.Find(item.Path);
                var afterInitialInsert = DateTime.UtcNow.AddSeconds(1)
                    .TruncateMilliseconds();
                Thread.Sleep(1200);
                var beforeUpsert = DateTime.UtcNow.TruncateMilliseconds();
                arena.SUT.Upsert(second);
                var afterUpsert = arena.SUT.Find(item.Path);
                Console.WriteLine(new
                {
                    initialDb,
                    afterUpsert,
                    afterInitialInsert
                }.Stringify());
                // Assert
                using var conn = arena.OpenConnection();
                var result = conn.Query<HistoryItem>($"select * from {Table.NAME};")
                    .ToArray();
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Item("Should have 1 result");
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(inDb => inDb.Path == item.Path &&
                                        inDb.Size == second.Size &&
                                        inDb.Created == initialDb.Created &&
                                        inDb.Modified != null &&
                                        inDb.Modified.Value > initialDb.Modified.Value,
                        () =>
                            $"Single result should match input\n{second.Stringify()}\nvs\n{result[0].Stringify()}\nbeforeTest:{beforeTest}");
            }
        }

        [TestFixture]
        public class Find
        {
            [Test]
            public void WhenItemDoesNotExist_ShouldReturnNull()
            {
                // Arrange
                var path = GetRandomWindowsPath();
                using var arena = Create();
                // Act
                var result = arena.SUT.Find(path);
                // Assert
                Expect(result)
                    .To.Be.Null();
            }

            [Test]
            public void WhenItemDoesExist_ShouldReturnIt()
            {
                // Arrange
                var item = GetRandom<HistoryItem>();

                using var arena = Create();
                // Act
                arena.SUT.Upsert(item);
                var result = arena.SUT.Find(item.Path);
                // Assert
                Expect(result)
                    .Not.To.Be.Null();
                Expect(result)
                    .To.Match(item);
            }
        }

        [TestFixture]
        public class FindAll
        {
            [Test]
            public void GivenValidWildcard_ShouldReturnAllMatches()
            {
                // Arrange
                var series = GetRandomString(4);
                var other = GetAnother(series);
                var item1 = new HistoryItem($"{series}/Season 01/S01E03 Some Episode.avi", GetRandomInt());
                var item2 = new HistoryItem($"{series}/Season 02/S02E04 Another Episode.mkv", GetRandomInt());
                var nonMatched = new HistoryItem($"{other}/Season 01/S01E01 Moo.mkv", GetRandomInt());
                using var arena = Create();
                var sut = arena.SUT;
                new[] { item1, item2, nonMatched }
                    .ForEach(item => sut.Upsert(item));
                // Act
                var result = sut.FindAll($"{series}/*");
                // Assert
                Expect(result)
                    .To.Contain
                    .Only(2)
                    .Items();
                Expect(result)
                    .To.Contain
                    .Exactly(1)
                    .Matched.By(i => i.Path == item1.Path);
                Expect(result)
                    .To.Contain
                    .Exactly(1)
                    .Matched.By(i => i.Path == item2.Path);
            }

            [Test]
            public void ShouldAllowPercentageInPath()
            {
                // Arrange
                var series = GetRandomString(4);
                var other = GetAnother(series);
                var item1 = new HistoryItem($"{series}/%Season 01/S01E03 Some Episode.avi", GetRandomInt());
                var item2 = new HistoryItem($"{series}/Season 02/S02E04 Another Episode.mkv", GetRandomInt());
                using var arena = Create();
                var sut = arena.SUT;
                new[] { item1, item2 }
                    .ForEach(item => sut.Upsert(item));
                // Act
                var result = sut.FindAll($"{series}/%*");
                // Assert
                Expect(result)
                    .To.Contain
                    .Only(1)
                    .Items();
                Expect(result)
                    .To.Contain
                    .Exactly(1)
                    .Matched.By(i => i.Path == item1.Path);
            }
        }

        [TestFixture]
        public class Exists
        {
            [Test]
            public void WhenPathIsNotKnown_ShouldReturnFalse()
            {
                // Arrange
                var path = GetRandomWindowsPath();
                using var arena = Create();
                // Act
                var result = arena.SUT.Exists(path);
                // Assert
                Expect(result)
                    .To.Be.False();
            }

            [Test]
            public void WhenPathIsKnown_ShouldReturnFalse()
            {
                // Arrange
                var item = GetRandom<HistoryItem>();
                using var arena = Create();
                // Act
                arena.SUT.Upsert(item);
                var result = arena.SUT.Exists(item.Path);
                // Assert
                Expect(result)
                    .To.Be.True();
            }
        }

        private class TestArena : IDisposable
        {
            public IDbConnection OpenConnection() => _dbDatabase.Connect();
            public string Folder => _folder.Path;
            private AutoTempFolder _folder;
            private SqLiteDatabase _dbDatabase;
            public ITargetHistoryRepository SUT { get; }

            public TestArena()
            {
                _folder = new AutoTempFolder();
                _dbDatabase = new SqLiteDatabase(_folder.Path);
                SUT = Create(_folder);
            }

            public void Dispose()
            {
                _folder.Dispose();
                _folder = null;
            }
        }

        private static TestArena Create()
        {
            return new TestArena();
        }

        private static IDbConnection OpenBitsplatConnectionIn(
            AutoTempFolder folder)
        {
            return OpenBitsplatConnectionIn(folder.Path);
        }

        private static IDbConnection OpenBitsplatConnectionIn(
            string folderPath)
        {
            var factory = new SqLiteDatabase(folderPath);
            return factory.Connect();
        }

        private static ITargetHistoryRepository Create(
            AutoTempFolder folder
        )
        {
            return Create(folder.Path);
        }

        private static ITargetHistoryRepository Create(
            string targetFolder,
            IMessageWriter messageWriter = null
        )
        {
            return new TargetHistoryRepository(
                messageWriter ?? Substitute.For<IMessageWriter>(),
                targetFolder
            );
        }
    }

    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            new Bootstrapper().Init();
        }
    }
}