using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
using Dapper;
using PeanutButter.Utils;
using Table = bitsplat.Migrations.Constants.Tables.History;
using Columns = bitsplat.Migrations.Constants.Tables.History.Columns;

// ReSharper disable AccessToDisposedClosure

namespace bitsplat.History
{
    public class TargetHistoryRepository : ITargetHistoryRepository
    {
        public string DatabaseFile { get; }

        public const string DB_NAME = ".bitsplat.db";
        private readonly string _folder;
        private readonly string _databaseName;
        private readonly IMessageWriter _messageWriter;
        private string _connectionString;

        private string ConnectionString =>
            _connectionString ??= CreateConnectionString();

        public TargetHistoryRepository(
            IMessageWriter messageWriter,
            string folder,
            string databaseName = DB_NAME)
        {
            _folder = folder;
            _databaseName = databaseName;
            _messageWriter = messageWriter;
            DatabaseFile = Path.Combine(_folder, _databaseName);
            CreateDatabase();
            MigrateUp();
        }

        public void Upsert(IHistoryItem item)
        {
            Upsert(new[] { item });
        }

        public void Upsert(IEnumerable<IHistoryItem> items)
        {
            using var conn = OpenConnection();
            using var transaction = conn.BeginTransaction();
            items.ForEach(item =>
            {
                var existing = conn.QueryFirstOrDefault<HistoryItem>(
                    $"select * from {Table.NAME} where {Columns.PATH} = @Path;",
                    new { item.Path }
                );
                if (existing == null)
                {
                    InsertNewFor(conn, item);
                }
                else
                {
                    UpdateExisting(conn, existing, item);
                }
            });
            transaction.Commit();
        }

        private static void UpdateExisting(IDbConnection conn,
            IHistoryItem existing,
            IHistoryItem updated)
        {
            conn.Execute(
                $@"update {
                    Table.NAME
                } set
                    Size = @Size,
                    Modified = datetime('now')
                   where id = @Id;",
                new
                {
                    updated.Size,
                    existing.Id
                }
            );
        }

        private static void InsertNewFor(IDbConnection conn, IHistoryItem item)
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

        public HistoryItem Find(string path)
        {
            using var conn = OpenConnection();
            return conn.QueryFirstOrDefault<HistoryItem>(
                $"select * from {Table.NAME} where path = @path;",
                new
                {
                    path
                }
            );
        }

        public bool Exists(string path)
        {
            using var conn = OpenConnection();
            return conn.QueryFirstOrDefault<int>(
                    $"select id from {Table.NAME} where path = @path;",
                    new
                    {
                        path
                    }
                ) >
                0;
        }

        public IEnumerable<HistoryItem> FindAll(string match)
        {
            using var conn = OpenConnection();
            return conn.Query<HistoryItem>(
                $"select * from {Table.NAME} where path like @path ESCAPE '\\';",
                new
                {
                    path = match
                        .Replace("%", "\\%")
                        .Replace("*", "%")
                });
        }

        private void MigrateUp()
        {
            var runner = new EasyRunner(
                ConnectionString,
                GetType()
                    .Assembly
            );
            try
            {
                runner.MigrateUp();
            }
            catch
            {
                _messageWriter.Write(
                    $"Failed to create history database; if the target is on an SMB share, try mounting with 'nobrl'"
                );
                throw;
            }
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
            var uri = new Uri(DatabaseFile).ToString();
            ;
            if (Platform.IsWindows)
            {
                uri = uri.Replace("file:///", "file://");
            }

            return new SQLiteConnectionStringBuilder()
            {
                Uri = uri
            }.ToString();
        }
    }
}