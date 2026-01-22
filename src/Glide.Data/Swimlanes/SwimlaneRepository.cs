using System;
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

        IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name, BoardId = boardId, Position = position });
        return new Swimlane(id, name, boardId, position);
    }

    public async Task CreateDefaultSwimlanesAsync(string boardId)
    {
        await CreateAsync("Todo", boardId, 0);
        await CreateAsync("In Progress", boardId, 1);
        await CreateAsync("Done", boardId, 2);
    }
}

public record Swimlane(string Id, string Name, string BoardId, int Position);