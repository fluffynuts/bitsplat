using System.Data;
using System.Data.SQLite;
using System.IO;
using Dapper;
using FluentMigrator.Runner.Processors.SQLite;
using Table = bitsplat.Migrations.Constants.Tables.History;
using Columns = bitsplat.Migrations.Constants.Tables.History.Columns;

namespace bitsplat
{
    public class BitsplatDbMigrationRunner : DbMigrationsRunner<SQLiteProcessorFactory>
    {
        public BitsplatDbMigrationRunner(
            string connectionString)
            : base(
                typeof(BitsplatDbMigrationRunner).Assembly,
                connectionString)
        {
        }
    }

    public interface ITargetHistoryRepository
    {
        void Add(History item);
        History Find(string path);
        bool Exists(string path);
    }

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

        public void Add(History item)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(
                    $@"insert or ignore into {
                            Table.NAME
                        } ({
                            Columns.PATH
                        }, {
                            Columns.SIZE
                        }) 
                    values (@Path, @Size);",
                    item);
            }
        }

        public History Find(string path)
        {
            using (var conn = OpenConnection())
            {
                return conn.QueryFirstOrDefault<History>(
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
                ) > 0;
            }
        }

        private void MigrateUp()
        {
            var runner = new BitsplatDbMigrationRunner(ConnectionString);
            runner.MigrateToLatest();
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