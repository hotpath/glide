using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Swimlanes;

public class SwimlaneRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<Swimlane> CreateAsync(string name, string boardId, int position)
    {
        string id = Guid.CreateVersion7().ToString();

        const string statement =
            "INSERT INTO swimlanes(id, name, board_id, position) VALUES(@Id,@Name,@BoardId,@Position)";

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name, BoardId = boardId, Position = position });
        return new Swimlane { Id = id, Name = name, BoardId = boardId, Position = position };
    }

    public async Task CreateDefaultSwimlanesAsync(string boardId)
    {
        await CreateAsync("Todo", boardId, 0);
        await CreateAsync("In Progress", boardId, 1);
        await CreateAsync("Done", boardId, 2);
    }

    public async Task<Swimlane?> GetByIdAsync(string id)
    {
        string query = """
                       SELECT s.id, s.name, s.board_id, s.position,
                              t.id, t.title, t.description, t.board_id, t.swimlane_id, t.assigned_to, t.position
                       FROM swimlanes AS s
                       LEFT JOIN tasks AS t ON s.id = t.swimlane_id
                       WHERE s.id = @Id     
                       """;

        using IDbConnection conn = connectionFactory.CreateConnection();

        return await conn.QuerySingleAsync<Swimlane>(query,
            new { Id = id });
    }

    public async Task<IEnumerable<Swimlane>> GetAllByBoardIdAsync(string boardId)
    {
        string query = """
                       SELECT s.id, s.name, s.board_id, s.position,
                              t.id, t.title, t.description, t.board_id, t.swimlane_id, t.assigned_to, t.position
                       FROM swimlanes AS s
                       LEFT JOIN tasks AS t ON s.id = t.swimlane_id
                       WHERE s.board_id = @BoardId
                       ORDER BY s.position
                       """;

        using IDbConnection conn = connectionFactory.CreateConnection();

        Dictionary<string, Swimlane> swimlaneLookup = new();
        await conn.QueryAsync<Swimlane, Tasks.Task?, Swimlane>(query,
            (swimlane, task) =>
            {
                if (!swimlaneLookup.TryGetValue(swimlane.Id, out Swimlane? existing))
                {
                    existing = swimlane;
                    existing.Tasks = new List<Tasks.Task>();
                    swimlaneLookup.Add(swimlane.Id, existing);
                }

                if (task is not null)
                {
                    ((List<Tasks.Task>)existing.Tasks).Add(task);
                }

                return existing;
            },
            new { BoardId = boardId },
            splitOn: "id");

        return swimlaneLookup.Values;
    }
}

public record Swimlane
{
    public string Id { get; set; } = "";
    public string BoardId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Position { get; set; }
    public IEnumerable<Tasks.Task> Tasks { get; set; } = [];
}