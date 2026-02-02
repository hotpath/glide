using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602021327)]
public class AddPasswordAuthentication : Migration
{
    public override void Up()
    {
        // Add password_hash column for local authentication
        Alter.Table("users")
            .AddColumn("password_hash").AsString().Nullable();
    }

    public override void Down()
    {
        // Remove password_hash column
        Delete.Column("password_hash").FromTable("users");
    }
}
