using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202601300001)]
public class RenameTasksToCards : Migration
{
    public override void Up()
    {
        // SQLite doesn't support renaming tables directly with constraints
        // We need to use the CREATE/INSERT/DROP pattern

        // 1. Rename tasks table to cards
        Rename.Table("tasks").To("cards");

        // 2. Rename task_labels table to card_labels
        Rename.Table("task_labels").To("card_labels");

        // 3. Update indexes for cards table (delete old, create new)
        Delete.Index("idx_boards_board_id").OnTable("cards");
        Delete.Index("idx_tasks_swimlane_id").OnTable("cards");
        Delete.Index("idx_boards_assigned_to").OnTable("cards");

        Create.Index("idx_cards_board_id").OnTable("cards").OnColumn("board_id");
        Create.Index("idx_cards_swimlane_id").OnTable("cards").OnColumn("swimlane_id");
        Create.Index("idx_cards_assigned_to").OnTable("cards").OnColumn("assigned_to");

        // 4. Update indexes for card_labels table (delete old, create new)
        Delete.Index("idx_task_labels_task_id").OnTable("card_labels");
        Delete.Index("idx_task_labels_label_id").OnTable("card_labels");

        Create.Index("idx_card_labels_card_id").OnTable("card_labels").OnColumn("task_id");
        Create.Index("idx_card_labels_label_id").OnTable("card_labels").OnColumn("label_id");

        // 5. Rename column in card_labels table from task_id to card_id
        Rename.Column("task_id").OnTable("card_labels").To("card_id");
    }

    public override void Down()
    {
        // Reverse the migration

        // 1. Rename column back
        Rename.Column("card_id").OnTable("card_labels").To("task_id");

        // 2. Revert card_labels indexes
        Delete.Index("idx_card_labels_card_id").OnTable("card_labels");
        Delete.Index("idx_card_labels_label_id").OnTable("card_labels");

        Create.Index("idx_task_labels_task_id").OnTable("card_labels").OnColumn("task_id");
        Create.Index("idx_task_labels_label_id").OnTable("card_labels").OnColumn("label_id");

        // 3. Rename table back
        Rename.Table("card_labels").To("task_labels");

        // 4. Revert cards indexes
        Delete.Index("idx_cards_board_id").OnTable("cards");
        Delete.Index("idx_cards_swimlane_id").OnTable("cards");
        Delete.Index("idx_cards_assigned_to").OnTable("cards");

        Create.Index("idx_boards_board_id").OnTable("cards").OnColumn("board_id");
        Create.Index("idx_tasks_swimlane_id").OnTable("cards").OnColumn("swimlane_id");
        Create.Index("idx_boards_assigned_to").OnTable("cards").OnColumn("assigned_to");

        // 5. Rename table back
        Rename.Table("cards").To("tasks");
    }
}
