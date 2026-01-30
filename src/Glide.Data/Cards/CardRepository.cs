using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Cards;

public class CardRepository(IDbConnectionFactory connectionFactory) : ICardRepository
{
    public async Task<Card> CreateAsync(string title, string boardId, string columnId)
    {
        string id = Guid.CreateVersion7().ToString();
        const string statement = """
                                 INSERT INTO cards(id, title, board_id, column_id)
                                 VALUES (@Id, @Title, @BoardId, @ColumnId)
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Title = title, BoardId = boardId, ColumnId = columnId });

        return new Card { Id = id, Title = title, BoardId = boardId, ColumnId = columnId };
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

    private async Task<int> GetNextPositionForColumn(string columnId)
    {
        const string query = "SELECT MAX(position) as Position FROM cards WHERE column_id = @ColumnId";
        using IDbConnection conn = connectionFactory.CreateConnection();

        int? maxPosition = await conn.QuerySingleOrDefaultAsync<int?>(query, new { ColumnId = columnId });
        return maxPosition.HasValue ? maxPosition.Value + 1 : 0;
    }

    private async System.Threading.Tasks.Task UpdatePositions(string column, int startingPosition)
    {
        const string statement = """
                                 UPDATE cards
                                 SET position = position + 1
                                 WHERE column_id = @Column
                                 AND position > @Position
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Column = column, Position = startingPosition });
    }

    public async System.Threading.Tasks.Task MoveToColumnAsync(string id, string columnId, int? position = null)
    {
        int nextPosition = position ?? await GetNextPositionForColumn(columnId);
        const string statement = """
                                 UPDATE cards
                                 SET column_id=@ColumnId, position=@Position
                                 WHERE id = @Id
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();

        await UpdatePositions(columnId, nextPosition);

        await conn.ExecuteAsync(statement, new { ColumnId = columnId, Position = nextPosition, Id = id });
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
    public string? ColumnId { get; set; }
    public string? AssignedTo { get; set; }
    public int Position { get; set; }
}
