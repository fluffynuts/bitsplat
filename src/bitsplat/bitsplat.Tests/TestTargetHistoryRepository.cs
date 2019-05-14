using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;
using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

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
                    Expect(() => conn.Open()).Not.To.Throw();
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
                        Uri = Path.Combine(folder.Path, ".bitsplat.db")
                    };
                    using (var conn = new SQLiteConnection(builder.ToString())
                        .OpenAndReturn())
                    {
                        Expect(() => conn.Query<History>("select * from history;"))
                            .Not.To.Throw();
                    }
                }
            }
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
}