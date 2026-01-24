using System.Data;

using FluentMigrator;

namespace Glide.Data.Migrations;

[Migration(202601220323)]
public class CreateInitialSchema : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("display_name").AsString()
            .WithColumn("email").AsString().NotNullable().Unique()
            .WithColumn("oauth_provider").AsString()
            .WithColumn("oauth_provider_id").AsString()
            .WithColumn("created_at").AsInt64()
            .WithColumn("updated_at").AsInt64();

        Create.Index("idx_users_oauth_provider").OnTable("users").OnColumn("oauth_provider").Ascending()
            .OnColumn("oauth_provider_id");


        Create.Table("boards")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("name").AsString().NotNullable();

        Create.Table("boards_users")
            .WithColumn("board_id").AsString().NotNullable().ForeignKey("fk_board_id_boards", "boards", "id")
            .OnDelete(Rule.Cascade)
            .Indexed("idx_boards_users_board_id")
            .WithColumn("user_id").AsString().NotNullable().ForeignKey("fk_user_id_users", "users", "id")
            .OnDelete(Rule.Cascade)
            .Indexed("idx_boards_users_user_id")
            .WithColumn("is_owner").AsInt16().NotNullable().WithDefaultValue(0).Indexed("idx_boards_users_is_owner");

        Create.UniqueConstraint("uniq_boards_users").OnTable("boards_users").Columns("board_id", "user_id");

        Create.Table("swimlanes")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("board_id").AsString().NotNullable().ForeignKey("fk_board_id_boards", "boards", "id")
            .OnDelete(Rule.Cascade)
            .Indexed("idx_swimlanes_board_id")
            .WithColumn("position").AsInt32().NotNullable().WithDefaultValue(0);

        Create.Table("tasks")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("description").AsString().NotNullable()
            .WithColumn("board_id").AsString().NotNullable().ForeignKey("fk_board_id_boards", "boards", "id")
            .OnDelete(Rule.Cascade)
            .Indexed("idx_boards_board_id")
            .WithColumn("assigned_to").AsString().ForeignKey("fk_assigned_to_users", "users", "id")
            .WithColumn("swimlane_id").AsString().ForeignKey("fk_swimlane_id_swimlanes", "swimlanes", "id")
            .Indexed("idx_tasks_swimlane_id")
            .WithColumn("position").AsInt32().NotNullable().WithDefaultValue(0)
            .Indexed("idx_boards_assigned_to");

        Create.Table("sessions")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("user_id").AsString().NotNullable().ForeignKey("fk_user_id_users", "users", "id")
            .OnDelete(Rule.Cascade)
            .Indexed("idx_sessions_user_id")
            .WithColumn("created_at").AsInt64().NotNullable()
            .WithColumn("expires_at").AsInt64().NotNullable().Indexed("idx_sessions_expires_at");

        Create.Table("labels")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("board_id").AsString().NotNullable().ForeignKey("fk_board_id_boards", "boards", "id")
            .OnDelete(Rule.Cascade).Indexed("idx_labels_board_id")
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("color").AsString().NotNullable().WithDefaultValue("#808080")
            .WithColumn("icon").AsString();

        Execute.Sql("""
                                CREATE TABLE task_labels (
                                    task_id TEXT NOT NULL,
                                    label_id TEXT NOT NULL,
                                    PRIMARY KEY (task_id, label_id),
                                    FOREIGN KEY (task_id) REFERENCES tasks(id),
                                    FOREIGN KEY (label_id) REFERENCES labels(id)
                                )
                    """);

        Create.Index("idx_task_labels_task_id").OnTable("task_labels").OnColumn("task_id");
        Create.Index("idx_task_labels_label_id").OnTable("task_labels").OnColumn("label_id");
    }

    public override void Down()
    {
        Delete.Index("idx_task_labels_label_id").OnTable("task_labels");
        Delete.Index("idx_task_labels_task_id").OnTable("task_labels");
        Delete.Table("task_labels");
        Delete.Table("labels");
        Delete.Table("sessions");
        Delete.Table("tasks");
        Delete.Table("swimlanes");
        Delete.Table("boards");
        Delete.UniqueConstraint("uniq_boards_users").FromTable("boards_users");
        Delete.Table("boards_users");
        Delete.Index("idx_users_oauth_provider");
        Delete.Table("users");
    }
}