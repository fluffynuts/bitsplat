using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace bitsplat.History
{
    public interface IDatabase
    {
        IDbConnection Connect();
        void MigrateUp();
    }
    
    public class SqLiteDatabase : IDatabase
    {
        private const string DbName = ".bitsplat.db";
        private readonly string _connectionString;

        public SqLiteDatabase(
            string dbFolder)
        {
            var pathToDbFile = Path.Combine(dbFolder, DbName);
            _connectionString = CreateConnectionString(pathToDbFile);
        }

        public IDbConnection Connect()
        {
            return new SQLiteConnection(
                _connectionString
            ).OpenAndReturn();
        }

        public void MigrateUp()
        {
            EnsureDatabaseExists();
            var runner = new EasyRunner(_connectionString, GetType().Assembly);
            runner.MigrateUp();
        }

        private void EnsureDatabaseExists()
        {
            using (Connect())
            {
            }
        }

        private string CreateConnectionString(string pathToDbFile)
        {
            return new SQLiteConnectionStringBuilder()
            {
                FullUri = new Uri(pathToDbFile).ToString()
            }.ToString();
        }
    }
}