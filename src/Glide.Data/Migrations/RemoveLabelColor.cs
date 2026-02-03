using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602031000)]
public class RemoveLabelColor : Migration
{
    public override void Up()
    {
        // Remove color column from labels table
        Delete.Column("color").FromTable("labels");
    }

    public override void Down()
    {
        // Add back color column with default value
        Alter.Table("labels")
            .AddColumn("color").AsString().NotNullable().WithDefaultValue("#808080");
    }
}