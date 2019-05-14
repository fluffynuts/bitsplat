using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors.SQLite;

namespace bitsplat
{

    public class BitsplatDbMigrationRunner : DbMigrationsRunner<SQLiteProcessorFactory>
    {
        public BitsplatDbMigrationRunner(
            string connectionString): base(
            typeof(BitsplatDbMigrationRunner).Assembly,
            connectionString)
        {
        }
    }

    public interface ITargetHistoryRepository
    {
    }

    public class TargetHistoryRepository : ITargetHistoryRepository
    {
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
                Uri = Path.Combine(_folder, ".bitsplat.db")
            }.ToString();
        }
    }
}