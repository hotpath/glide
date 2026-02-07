using System;

using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602071500)]
public class AddDateFormatSetting : Migration
{
    public override void Up()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Insert.IntoTable("site_settings")
            .Row(new
            {
                key = "date_format",
                value = "yyyy-MM-dd",
                created_at = now,
                updated_at = now
            });
    }

    public override void Down()
    {
        Delete.FromTable("site_settings")
            .Row(new { key = "date_format" });
    }
}
