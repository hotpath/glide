using System.Data;

using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202602020732)]
public class SeparateOAuthProviders : Migration
{
    public override void Up()
    {
        // Create user_oauth_providers table
        Create.Table("user_oauth_providers")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("user_id").AsString().NotNullable()
                .ForeignKey("fk_user_oauth_providers_user_id", "users", "id")
                .OnDelete(Rule.Cascade)
                .Indexed("idx_user_oauth_providers_user_id")
            .WithColumn("provider").AsString().NotNullable()
            .WithColumn("provider_user_id").AsString().NotNullable()
            .WithColumn("provider_email").AsString().Nullable()
            .WithColumn("created_at").AsInt64().NotNullable()
            .WithColumn("updated_at").AsInt64().NotNullable();

        // Create unique constraint on provider + provider_user_id
        Create.UniqueConstraint("uniq_provider_user")
            .OnTable("user_oauth_providers")
            .Columns("provider", "provider_user_id");

        // Create index on provider for faster lookups
        Create.Index("idx_user_oauth_providers_provider")
            .OnTable("user_oauth_providers")
            .OnColumn("provider");

        // Migrate existing data from users table to user_oauth_providers
        Execute.Sql(@"
            INSERT INTO user_oauth_providers (id, user_id, provider, provider_user_id, provider_email, created_at, updated_at)
            SELECT
                lower(hex(randomblob(16))),
                id,
                oauth_provider,
                oauth_provider_id,
                email,
                created_at,
                updated_at
            FROM users
            WHERE oauth_provider IS NOT NULL AND oauth_provider_id IS NOT NULL
        ");

        // Drop index first, before dropping columns
        Delete.Index("idx_users_oauth_provider").OnTable("users");

        // Remove old columns from users table
        Delete.Column("oauth_provider").FromTable("users");
        Delete.Column("oauth_provider_id").FromTable("users");
    }

    public override void Down()
    {
        // Add back oauth_provider columns to users table
        Alter.Table("users")
            .AddColumn("oauth_provider").AsString().Nullable()
            .AddColumn("oauth_provider_id").AsString().Nullable();

        Create.Index("idx_users_oauth_provider")
            .OnTable("users")
            .OnColumn("oauth_provider").Ascending()
            .OnColumn("oauth_provider_id");

        // Migrate data back (taking the first provider for each user)
        Execute.Sql(@"
            UPDATE users
            SET
                oauth_provider = (
                    SELECT provider
                    FROM user_oauth_providers
                    WHERE user_id = users.id
                    LIMIT 1
                ),
                oauth_provider_id = (
                    SELECT provider_user_id
                    FROM user_oauth_providers
                    WHERE user_id = users.id
                    LIMIT 1
                )
            WHERE EXISTS (
                SELECT 1
                FROM user_oauth_providers
                WHERE user_id = users.id
            )
        ");

        // Drop user_oauth_providers table
        Delete.Index("idx_user_oauth_providers_provider").OnTable("user_oauth_providers");
        Delete.UniqueConstraint("uniq_provider_user").FromTable("user_oauth_providers");
        Delete.Index("idx_user_oauth_providers_user_id").OnTable("user_oauth_providers");
        Delete.Table("user_oauth_providers");
    }
}