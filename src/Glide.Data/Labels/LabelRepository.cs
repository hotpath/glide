using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Labels;

public class LabelRepository(IDbConnectionFactory connectionFactory) : ILabelRepository
{
    public async Task<Label> CreateAsync(string boardId, string name, string? icon)
    {
        string id = Guid.CreateVersion7().ToString();
        const string statement = """
                                 INSERT INTO labels(id, board_id, name, icon)
                                 VALUES (@Id, @BoardId, @Name, @Icon)
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, BoardId = boardId, Name = name, Icon = icon });

        return new Label { Id = id, BoardId = boardId, Name = name, Icon = icon };
    }

    public async Task<Label?> GetByIdAsync(string id)
    {
        const string query = "SELECT * FROM labels WHERE id = @Id";
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Label>(query, new { Id = id });
    }

    public async Task<IEnumerable<Label>> GetByBoardIdAsync(string boardId)
    {
        const string query = "SELECT * FROM labels WHERE board_id = @BoardId ORDER BY name";
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Label>(query, new { BoardId = boardId });
    }

    public async Task UpdateAsync(string id, string name, string? icon)
    {
        const string statement = """
                                 UPDATE labels
                                 SET name = @Name,
                                     icon = @Icon
                                 WHERE id = @Id
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name, Icon = icon });
    }

    public async Task DeleteAsync(string id)
    {
        const string statement = "DELETE FROM labels WHERE id = @Id";

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id });
    }

    public async Task AddLabelToCardAsync(string cardId, string labelId)
    {
        const string statement = """
                                 INSERT OR IGNORE INTO card_labels(card_id, label_id)
                                 VALUES (@CardId, @LabelId)
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { CardId = cardId, LabelId = labelId });
    }

    public async Task RemoveLabelFromCardAsync(string cardId, string labelId)
    {
        const string statement = """
                                 DELETE FROM card_labels
                                 WHERE card_id = @CardId AND label_id = @LabelId
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { CardId = cardId, LabelId = labelId });
    }

    public async Task<IEnumerable<Label>> GetLabelsByCardIdAsync(string cardId)
    {
        const string query = """
                             SELECT l.*
                             FROM labels l
                             INNER JOIN card_labels cl ON l.id = cl.label_id
                             WHERE cl.card_id = @CardId
                             ORDER BY l.name
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Label>(query, new { CardId = cardId });
    }

    public async Task<IEnumerable<string>> GetCardIdsByLabelIdAsync(string labelId)
    {
        const string query = """
                             SELECT card_id
                             FROM card_labels
                             WHERE label_id = @LabelId
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<string>(query, new { LabelId = labelId });
    }
}

public record Label
{
    public string Id { get; set; } = "";
    public string BoardId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
}
