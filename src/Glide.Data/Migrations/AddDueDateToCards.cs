using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602071400)]
public class AddDueDateToCards : Migration
{
    public override void Up()
    {
        Alter.Table("cards")
            .AddColumn("due_date").AsString(10).Nullable();
    }

    public override void Down()
    {
        Delete.Column("due_date").FromTable("cards");
    }
}