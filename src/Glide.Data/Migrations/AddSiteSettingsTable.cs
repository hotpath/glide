using System;

using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602071300)]
public class AddSiteSettingsTable : Migration
{
    public override void Up()
    {
        Create.Table("site_settings")
            .WithColumn("key").AsString(255).NotNullable().PrimaryKey()
            .WithColumn("value").AsString(1000).NotNullable()
            .WithColumn("created_at").AsInt64().NotNullable()
            .WithColumn("updated_at").AsInt64().NotNullable();

        // Insert default value for registration_open
        Insert.IntoTable("site_settings")
            .Row(new
            {
                key = "registration_open",
                value = "true",
                created_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
    }

    public override void Down()
    {
        Delete.Table("site_settings");
    }
}