using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202601300002)]
public class RenameSwimlanesToColumns : Migration
{
    public override void Up()
    {
        // 1. Rename swimlanes table to columns
        Rename.Table("swimlanes").To("columns");

        // 2. Update indexes for columns table (delete old, create new)
        Delete.Index("idx_swimlanes_board_id").OnTable("columns");
        Create.Index("idx_columns_board_id").OnTable("columns").OnColumn("board_id");

        // 3. Rename swimlane_id column in cards table to column_id
        Rename.Column("swimlane_id").OnTable("cards").To("column_id");

        // 4. Update indexes for cards table
        Delete.Index("idx_cards_swimlane_id").OnTable("cards");
        Create.Index("idx_cards_column_id").OnTable("cards").OnColumn("column_id");
    }

    public override void Down()
    {
        // Reverse the migration

        // 1. Revert cards indexes
        Delete.Index("idx_cards_column_id").OnTable("cards");
        Create.Index("idx_cards_swimlane_id").OnTable("cards").OnColumn("column_id");

        // 2. Rename column back
        Rename.Column("column_id").OnTable("cards").To("swimlane_id");

        // 3. Revert columns indexes
        Delete.Index("idx_columns_board_id").OnTable("columns");
        Create.Index("idx_swimlanes_board_id").OnTable("columns").OnColumn("board_id");

        // 4. Rename table back
        Rename.Table("columns").To("swimlanes");
    }
}
