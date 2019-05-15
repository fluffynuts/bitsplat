using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;
using static NExpect.Expectations;
using NExpect;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
using Table = bitsplat.Migrations.Constants.Tables.History;
using NExpect.Implementations;
using PeanutButter.RandomGenerators;

namespace bitsplat.Tests
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
                using (var folder = new AutoTempFolder())
                {
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
                        Uri = Path.Combine(folder.Path, files.First())
                    };
                    var conn = new SQLiteConnection(builder.ToString());
                    Expect(() => conn.Open())
                        .Not.To.Throw();
                }
            }

            [Test]
            public void WhenTargetDatabaseMissing_ShouldAddHistoryTable()
            {
                // Arrange
                using (var folder = new AutoTempFolder())
                {
                    Expect(folder)
                        .Not.To.Have.Contents();
                    // Act
                    Create(folder);
                    // Assert
                    Expect(folder)
                        .To.Have.Contents();
                    var builder = new SQLiteConnectionStringBuilder()
                    {
                        Uri = Path.Combine(folder.Path, TargetHistoryRepository.DB_NAME)
                    };
                    using (var conn = new SQLiteConnection(builder.ToString())
                        .OpenAndReturn())
                    {
                        Expect(() =>
                                conn.Query<History>($"select * from {Table.NAME};")
                            )
                            .Not.To.Throw();
                    }
                }
            }

            [Test]
            public void ShouldMakeHistoryPathUnique()
            {
                // Arrange
                var path = GetRandomWindowsPath();
                var size1 = GetRandomInt();
                var size2 = GetRandomInt();
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
            }
        }

        [TestFixture]
        public class Add
        {
            [Test]
            public void ShouldAddANewHistoryItem()
            {
                // Arrange
                var item = GetRandom<History>();
                var beforeTest = DateTime.Now.TruncateMilliseconds();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Add(item);
                    // Assert
                    using (var conn = arena.OpenConnection())
                    {
                        var result = conn.Query<History>($"select * from {Table.NAME};")
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
                }
            }

            [Test]
            public void ShouldNotAddRepeatedPath()
            {
                // Arrange
                var item = GetRandom<History>();
                var second = GetRandom<History>();
                second.Path = item.Path;
                var beforeTest = DateTime.Now.TruncateMilliseconds();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Add(item);
                    arena.SUT.Add(second);
                    // Assert
                    using (var conn = arena.OpenConnection())
                    {
                        var result = conn.Query<History>($"select * from {Table.NAME};")
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
                var path = GetRandomWindowsPath();
                using (var arena = Create())
                {
                    // Act
                    var result = arena.SUT.Find(path);
                    // Assert
                    Expect(result)
                        .To.Be.Null();
                }
            }

            [Test]
            public void WhenItemDoesExist_ShouldReturnIt()
            {
                // Arrange
                var item = GetRandom<History>();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Add(item);
                    var result = arena.SUT.Find(item.Path);
                    // Assert
                    Expect(result)
                        .Not.To.Be.Null();
                    Expect(result).To.Match(item);
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
                var path = GetRandomWindowsPath();
                using (var arena = Create())
                {
                    // Act
                    var result = arena.SUT.Exists(path);
                    // Assert
                    Expect(result).To.Be.False();
                }
            }
            
            [Test]
            public void WhenPathIsKnown_ShouldReturnFalse()
            {
                // Arrange
                var item = GetRandom<History>();
                using (var arena = Create())
                {
                    // Act
                    arena.SUT.Add(item);
                    var result = arena.SUT.Exists(item.Path);
                    // Assert
                    Expect(result).To.Be.True();
                }
            }
        }

        private class TestArena : IDisposable
        {
            public IDbConnection OpenConnection() =>
                new SQLiteConnection(
                    new SQLiteConnectionStringBuilder()
                    {
                        Uri = Path.Combine(Folder, TargetHistoryRepository.DB_NAME)
                    }.ToString()
                ).OpenAndReturn();

            public string Folder => _folder.Path;
            private AutoTempFolder _folder;
            public ITargetHistoryRepository SUT { get; }

            public TestArena()
            {
                _folder = new AutoTempFolder();
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
            return new SQLiteConnection(
                new SQLiteConnectionStringBuilder()
                {
                    Uri = Path.Combine(folderPath, TargetHistoryRepository.DB_NAME)
                }.ToString()
            ).OpenAndReturn();
        }

        private static ITargetHistoryRepository Create(
            AutoTempFolder folder
        )
        {
            return Create(folder.Path);
        }

        private static ITargetHistoryRepository Create(
            string folder
        )
        {
            return new TargetHistoryRepository(folder);
        }
    }

    public static class HistoryMatchers
    {
        public static void Match(
            this ITo<History> to,
            History expected)
        {
            to.AddMatcher(actual =>
            {
                var passed = actual.Path == expected.Path &&
                             actual.Size == expected.Size &&
                             actual.Created >= expected.Created;
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual.Stringify()} {passed.AsNot()}to match {expected.Stringify()}"
                );
            });
        }
    }

    public class HistoryBuilder : GenericBuilder<HistoryBuilder, History>
    {
        public override HistoryBuilder WithRandomProps()
        {
            return base.WithRandomProps()
                .WithProp(o => o.Created = DateTime.Now);
        }
    }
}