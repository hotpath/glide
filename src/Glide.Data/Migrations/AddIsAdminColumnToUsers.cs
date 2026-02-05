using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602031200)]
public class AddIsAdminColumnToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("is_admin").AsInt16().NotNullable().WithDefaultValue(0)
            .Indexed("idx_users_is_admin");
    }

    public override void Down()
    {
        Delete.Index("idx_users_is_admin").OnTable("users");
        Delete.Column("is_admin").FromTable("users");
    }
}
