using FluentMigrator;
using Table = bitsplat.Migrations.Constants.Tables.History;
using Columns = bitsplat.Migrations.Constants.Tables.History.Columns;
// ReSharper disable InconsistentNaming

namespace bitsplat.Migrations
{
    [Migration(1)]
    public class Migration_01_CreateHistoryTable
        : Migration
    {
        public override void Up()
        {
            Create.Table(Table.NAME)
                .WithColumn(Columns.ID)
                .AsInt32()
                .PrimaryKey()
                .Identity()
                
                .WithColumn(Columns.PATH)
                .AsString(1024)
                .Unique()
                .NotNullable()
                
                .WithColumn(Columns.SIZE)
                .AsInt64()
                .NotNullable()
                
                .WithColumn(Columns.CREATED)
                .AsDateTime()
                .NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
                
                .WithColumn(Columns.MODIFIED)
                .AsDateTime()
                .Nullable();
        }

        public override void Down()
        {
            /* do nothing */
        }
    }
}