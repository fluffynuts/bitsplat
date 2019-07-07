using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using Dapper;
using Table = bitsplat.Migrations.Constants.Tables.History;
using Columns = bitsplat.Migrations.Constants.Tables.History.Columns;

namespace bitsplat.History
{
    public class TargetHistoryRepository : ITargetHistoryRepository
    {
        public const string DB_NAME = ".bitsplat.db";
        private readonly string _folder;
        private string _connectionString;

        private string ConnectionString =>
            _connectionString ?? (_connectionString = CreateConnectionString());

        public TargetHistoryRepository(
            string folder)
        {
            _folder = folder;
            CreateDatabase();
            MigrateUp();
        }

        public void Upsert(IHistoryItem item)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(
                    $@"replace into {
                            Table.NAME
                        } ({
                            Columns.PATH
                        }, {
                            Columns.SIZE
                        }, {
                            Columns.MODIFIED
                        })
                    values (@Path, @Size, datetime('now'));",
                    item);
            }
        }

        public HistoryItem Find(string path)
        {
            using (var conn = OpenConnection())
            {
                return conn.QueryFirstOrDefault<HistoryItem>(
                    $"select * from {Table.NAME} where path = @path;",
                    new
                    {
                        path
                    }
                );
            }
        }

        public bool Exists(string path)
        {
            using (var conn = OpenConnection())
            {
                return conn.QueryFirstOrDefault<int>(
                           $"select id from {Table.NAME} where path = @path;",
                           new
                           {
                               path
                           }
                       ) >
                       0;
            }
        }

        public IEnumerable<HistoryItem> FindAll(string match)
        {
            using (var conn = OpenConnection())
            {
                return conn.Query<HistoryItem>(
                    $"select * from {Table.NAME} where path like @path ESCAPE '\\';",
                    new
                    {
                        path = match
                            .Replace("%", "\\%")
                            .Replace("*", "%")
                    });
            }
        }

        private void MigrateUp()
        {
            var runner = new EasyRunner(ConnectionString,
                GetType()
                    .Assembly);
            runner.MigrateUp();
        }

        private void CreateDatabase()
        {
            using (OpenConnection())
            {
            }
        }

        private IDbConnection OpenConnection()
        {
            return new SQLiteConnection(ConnectionString)
                .OpenAndReturn();
        }

        private string CreateConnectionString()
        {
            return new SQLiteConnectionStringBuilder()
            {
                Uri = Path.Combine(_folder, DB_NAME)
            }.ToString();
        }
    }
}