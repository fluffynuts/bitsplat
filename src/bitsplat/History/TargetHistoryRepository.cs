using System.Data;
using Dapper;
using Table = bitsplat.Migrations.Constants.Tables.History;
using Columns = bitsplat.Migrations.Constants.Tables.History.Columns;

namespace bitsplat.History
{
    public class TargetHistoryRepository : ITargetHistoryRepository
    {
        private readonly IDatabase _connectionFactory;
        private readonly string _targetFolder;

        public TargetHistoryRepository(
            IDatabase connectionFactory,
            string targetFolder)
        {
            connectionFactory.MigrateUp();
            _connectionFactory = connectionFactory;
            _targetFolder = targetFolder;
        }

        public void Add(HistoryItem item)
        {
            using (var conn = _connectionFactory.Connect())
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

        public HistoryItem Find(string path)
        {
            using (var conn = _connectionFactory.Connect())
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
            using (var conn = _connectionFactory.Connect())
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
    }

}