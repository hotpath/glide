using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Tasks;

public class TaskRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<Task> CreateAsync(string title, string boardId, string swimlaneId)
    {
        string id = Guid.CreateVersion7().ToString();
        const string statement = """
                                 INSERT INTO tasks(id, title, board_id, swimlane_id)
                                 VALUES (@Id, @Title, @BoardId, @SwimlaneId)
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Title = title, BoardId = boardId, SwimlaneId = swimlaneId });

        return new Task { Id = id, Title = title, BoardId = boardId, SwimlaneId = swimlaneId };
    }

    public async Task<Task?> GetByIdAsync(string id)
    {
        const string query = "SELECT * FROM tasks WHERE id = @Id";
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Task>(query, new { Id = id });
    }

    public async System.Threading.Tasks.Task UpdateAsync(string id, string title, string? description)
    {
        const string statement = """
                                 UPDATE tasks
                                 SET title = @Title,
                                     description = @Description
                                 WHERE id = @Id
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Title = title, Description = description });
    }
}

public record Task
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string BoardId { get; set; } = "";
    public string? SwimlaneId { get; set; }
    public string? AssignedTo { get; set; }
    public int Position { get; set; }
}