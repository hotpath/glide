using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Cards;

public class CardRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<Card> CreateAsync(string title, string boardId, string swimlaneId)
    {
        string id = Guid.CreateVersion7().ToString();
        const string statement = """
                                 INSERT INTO cards(id, title, board_id, swimlane_id)
                                 VALUES (@Id, @Title, @BoardId, @SwimlaneId)
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Title = title, BoardId = boardId, SwimlaneId = swimlaneId });

        return new Card { Id = id, Title = title, BoardId = boardId, SwimlaneId = swimlaneId };
    }

    public async Task<Card?> GetByIdAsync(string id)
    {
        const string query = "SELECT * FROM cards WHERE id = @Id";
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Card>(query, new { Id = id });
    }

    public async System.Threading.Tasks.Task UpdateAsync(string id, string title, string? description)
    {
        const string statement = """
                                 UPDATE cards
                                 SET title = @Title,
                                     description = @Description
                                 WHERE id = @Id
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Title = title, Description = description });
    }

    private async Task<int> GetNextPositionForSwimlane(string swimlaneId)
    {
        const string query = "SELECT MAX(position) as Position FROM cards WHERE swimlane_id = @SwimlaneId";
        using IDbConnection conn = connectionFactory.CreateConnection();

        int? maxPosition = await conn.QuerySingleOrDefaultAsync<int?>(query, new { SwimlaneId = swimlaneId });
        return maxPosition.HasValue ? maxPosition.Value + 1 : 0;
    }

    private async System.Threading.Tasks.Task UpdatePositions(string swimlane, int startingPosition)
    {
        const string statement = """
                                 UPDATE cards
                                 SET position = position + 1
                                 WHERE swimlane_id = @Swimlane
                                 AND position > @Position
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Swimlane = swimlane, Position = startingPosition });
    }

    public async System.Threading.Tasks.Task MoveToSwimlaneAsync(string id, string swimlaneId, int? position = null)
    {
        int nextPosition = position ?? await GetNextPositionForSwimlane(swimlaneId);
        const string statement = """
                                 UPDATE cards
                                 SET swimlane_id=@SwimlaneId, position=@Position
                                 WHERE id = @Id
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();

        await UpdatePositions(swimlaneId, nextPosition);

        await conn.ExecuteAsync(statement, new { SwimlaneId = swimlaneId, Position = nextPosition, Id = id });
    }

    public async System.Threading.Tasks.Task DeleteAsync(string id)
    {
        const string statement = "DELETE FROM cards WHERE id = @Id";

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id });
    }
}

public record Card
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string BoardId { get; set; } = "";
    public string? SwimlaneId { get; set; }
    public string? AssignedTo { get; set; }
    public int Position { get; set; }
}
