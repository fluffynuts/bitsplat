using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using bitsplat.History;
using bitsplat.Tests.TestingSupport;
using Dapper;
using NExpect;
using NExpect.Implementations;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;
using Table = bitsplat.Migrations.Constants.Tables.History;

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
            Expectations.Expect(sut)
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
                using (var folder = new AutoTempFolder())
                {
                    Expectations.Expect(folder)
                        .Not.To.Have.Contents();
                    // Act
                    Create(folder);
                    // Assert
                    Expectations.Expect(folder)
                        .To.Have.Contents();
                    var files = Directory.GetFiles(folder.Path);
                    Expectations.Expect(files)
                        .To.Contain.Exactly(1)
                        .Item();
                    var builder = new SQLiteConnectionStringBuilder()
                    {
                        FullUri = new Uri(Path.Combine(folder.Path, files.First())).ToString()
                    };
                    var conn = new SQLiteConnection(builder.ToString());
                    Expectations.Expect(() => conn.Open())
                        .Not.To.Throw();
                }
            }

            [Test]
            public void WhenTargetDatabaseMissing_ShouldAddHistoryTable()
            {
                // Arrange
                using (var folder = new AutoTempFolder())
                {
                    Expectations.Expect(folder)
                        .Not.To.Have.Contents();
                    // Act
                    Create(folder);
                    // Assert
                    Expectations.Expect(folder)
                        .To.Have.Contents();
                    var dataLayer = new SqLiteDatabase(folder.Path);
                    using (var conn = dataLayer.Connect())
                    {
                        Expectations.Expect(() =>
                                conn.Query<bitsplat.History.HistoryItem>($"select * from {Table.NAME};")
                            )
                            .Not.To.Throw();
                    }
                }
            }

            [Test]
            public void ShouldMakeHistoryPathUnique()
            {
                // Arrange
                var path = RandomValueGen.GetRandomWindowsPath();
                var size1 = RandomValueGen.GetRandomInt();
                var size2 = RandomValueGen.GetRandomInt();
                using (var folder = new AutoTempFolder())
                {
                    // Act
                    Create(folder);
                    // Assert
                    using (var conn = OpenBitsplatConnectionIn(folder))
                    {
                        var sql = "insert into history (path, size) values (@path, @size)";
                        conn.Execute(sql,
                            new
                            {
                                path,
                                size = size1
                            });
                        Expectations.Expect(() =>
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
            }
        }

        [TestFixture]
        public class Upsert
        {
            [Test]
            public void ShouldAddANewHistoryItem()
            {
                // Arrange
                var item = RandomValueGen.GetRandom<bitsplat.History.HistoryItem>();
                var beforeTest = DateTime.UtcNow.TruncateMilliseconds();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Upsert(item);
                    // Assert
                    using (var conn = arena.OpenConnection())
                    {
                        var result = conn.Query<bitsplat.History.HistoryItem>($"select * from {Table.NAME};")
                            .ToArray();
                        Expectations.Expect(result)
                            .To.Contain.Exactly(1)
                            .Item("Should have 1 result");
                        Expectations.Expect(result)
                            .To.Contain.Exactly(1)
                            .Matched.By(inDb => inDb.Path == item.Path &&
                                    inDb.Size == item.Size &&
                                    inDb.Created >= beforeTest,
                                () =>
                                    $"Single result should match input\n{item.Stringify()}\nvs\n{result[0].Stringify()}\nbeforeTest:{beforeTest}");
                    }
                }
            }

            [Test]
            public void ShouldUpdateSizeAndSetLastModifiedOnRepeatedPath()
            {
                // Arrange
                var item = RandomValueGen.GetRandom<bitsplat.History.HistoryItem>();
                var second = RandomValueGen.GetRandom<bitsplat.History.HistoryItem>();
                second.Path = item.Path;
                var beforeTest = DateTime.UtcNow.TruncateMilliseconds();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Upsert(item);
                    var beforeUpsert = DateTime.UtcNow;
                    arena.SUT.Upsert(second);
                    // Assert
                    using (var conn = arena.OpenConnection())
                    {
                        var result = conn.Query<bitsplat.History.HistoryItem>($"select * from {Table.NAME};")
                            .ToArray();
                        Expectations.Expect(result)
                            .To.Contain.Exactly(1)
                            .Item("Should have 1 result");
                        Expectations.Expect(result)
                            .To.Contain.Exactly(1)
                            .Matched.By(inDb => inDb.Path == item.Path &&
                                    inDb.Size == second.Size &&
                                    inDb.Created >= beforeTest &&
                                                inDb.Modified != null && 
                                                inDb.Modified.Value.TruncateSeconds() >= beforeUpsert.TruncateSeconds() ,
                                () =>
                                    $"Single result should match input\n{second.Stringify()}\nvs\n{result[0].Stringify()}\nbeforeTest:{beforeTest}");
                    }
                }
            }
        }

        [TestFixture]
        public class Find
        {
            [Test]
            public void WhenItemDoesNotExist_ShouldReturnNull()
            {
                // Arrange
                var path = RandomValueGen.GetRandomWindowsPath();
                using (var arena = Create())
                {
                    // Act
                    var result = arena.SUT.Find(path);
                    // Assert
                    Expectations.Expect(result)
                        .To.Be.Null();
                }
            }

            [Test]
            public void WhenItemDoesExist_ShouldReturnIt()
            {
                // Arrange
                var item = RandomValueGen.GetRandom<bitsplat.History.HistoryItem>();

                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Upsert(item);
                    var result = arena.SUT.Find(item.Path);
                    // Assert
                    Expectations.Expect(result)
                        .Not.To.Be.Null();
                    Expectations.Expect(result).To.Match(item);
                }
            }
        }

        [TestFixture]
        public class Exists
        {
            [Test]
            public void WhenPathIsNotKnown_ShouldReturnFalse()
            {
                // Arrange
                var path = RandomValueGen.GetRandomWindowsPath();
                using (var arena = Create())
                {
                    // Act
                    var result = arena.SUT.Exists(path);
                    // Assert
                    Expectations.Expect(result).To.Be.False();
                }
            }

            [Test]
            public void WhenPathIsKnown_ShouldReturnFalse()
            {
                // Arrange
                var item = RandomValueGen.GetRandom<bitsplat.History.HistoryItem>();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Upsert(item);
                    var result = arena.SUT.Exists(item.Path);
                    // Assert
                    Expectations.Expect(result).To.Be.True();
                }
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
            string targetFolder
        )
        {
            return new TargetHistoryRepository(
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