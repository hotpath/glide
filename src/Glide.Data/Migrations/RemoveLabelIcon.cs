using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602031001)]
public class RemoveLabelIcon : Migration
{
    public override void Up()
    {
        // Remove icon column from labels table
        Delete.Column("icon").FromTable("labels");
    }

    public override void Down()
    {
        // Add back icon column
        Alter.Table("labels")
            .AddColumn("icon").AsString().Nullable();
    }
}
