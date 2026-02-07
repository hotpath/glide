using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Dapper;

using Glide.Data.Cards;

namespace Glide.Data.Columns;

public class ColumnRepository(IDbConnectionFactory connectionFactory) : IColumnRepository
{
    public async Task<Column> CreateAsync(string name, string boardId, int position)
    {
        string id = Guid.CreateVersion7().ToString();

        const string statement =
            "INSERT INTO columns(id, name, board_id, position) VALUES(@Id,@Name,@BoardId,@Position)";

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name, BoardId = boardId, Position = position });
        return new Column { Id = id, Name = name, BoardId = boardId, Position = position };
    }

    public async Task CreateDefaultColumnsAsync(string boardId)
    {
        await CreateAsync("To Do", boardId, 0);
        await CreateAsync("In Progress", boardId, 1);
        await CreateAsync("Done", boardId, 2);
    }

    public async Task<int> GetMaxPositionAsync(string boardId)
    {
        const string query = """
                             SELECT MAX(position) as maxPosition, COUNT(id) as count
                             FROM columns
                             WHERE board_id = @BoardId
                             """;
        using IDbConnection conn = connectionFactory.CreateConnection();

        MaxResult result = await conn.QuerySingleAsync<MaxResult>(query, new { BoardId = boardId });
        if (result.Count == 0 || result.MaxPosition is null)
        {
            return -1;
        }

        return (int)result.MaxPosition.Value; // we know that this is a 32-bit val
    }

    public async Task<Column?> GetByIdAsync(string id)
    {
        string query = """
                       SELECT c.id, c.name, c.board_id, c.position,
                              ca.id, ca.title, ca.description, ca.board_id, ca.column_id, ca.assigned_to, ca.position, ca.due_date
                       FROM columns AS c
                       LEFT JOIN cards AS ca ON c.id = ca.column_id
                       WHERE c.id = @Id
                       ORDER BY c.position, ca.position
                       """;

        using IDbConnection conn = connectionFactory.CreateConnection();

        Column? result = null;
        await conn.QueryAsync<Column, Card?, Column>(query,
            (column, card) =>
            {
                if (result is null)
                {
                    result = column;
                    result.Cards = new List<Card>();
                }

                if (card is not null)
                {
                    ((List<Card>)result.Cards).Add(card);
                }

                return result;
            },
            new { Id = id },
            splitOn: "id");

        return result;
    }

    public async Task<IEnumerable<Column>> GetAllByBoardIdAsync(string boardId)
    {
        string query = """
                       SELECT c.id, c.name, c.board_id, c.position,
                              ca.id, ca.title, ca.description, ca.board_id, ca.column_id, ca.assigned_to, ca.position, ca.due_date
                       FROM columns AS c
                       LEFT JOIN cards AS ca ON c.id = ca.column_id
                       WHERE c.board_id = @BoardId
                       ORDER BY c.position, ca.position
                       """;

        using IDbConnection conn = connectionFactory.CreateConnection();

        Dictionary<string, Column> columnLookup = new();
        await conn.QueryAsync<Column, Card?, Column>(query,
            (column, card) =>
            {
                if (!columnLookup.TryGetValue(column.Id, out Column? existing))
                {
                    existing = column;
                    existing.Cards = new List<Card>();
                    columnLookup.Add(column.Id, existing);
                }

                if (card is not null)
                {
                    ((List<Card>)existing.Cards).Add(card);
                }

                return existing;
            },
            new { BoardId = boardId },
            splitOn: "id");

        return columnLookup.Values;
    }

    public async Task DeleteAsync(string id)
    {
        const string statement = "DELETE FROM columns WHERE id=@Id";
        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id });
    }

    private record MaxResult
    {
        public long? MaxPosition { get; init; }
        public long Count { get; init; }
    }
}

public record Column
{
    public string Id { get; set; } = "";
    public string BoardId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Position { get; set; }
    public IEnumerable<Card> Cards { get; set; } = [];
}