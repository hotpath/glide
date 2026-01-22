using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Boards;

public class BoardRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<Board> CreateAsync(string name, string projectId)
    {
        string id = Guid.CreateVersion7().ToString();

        const string statement = "INSERT INTO boards(id, name, project_id) VALUES(@Id, @Name, @ProjectId)";

        IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name, ProjectId = projectId });

        return new Board(id, name, projectId);
    }

    public async Task<IEnumerable<Board>> GetByProjectIdAsync(string projectId)
    {
        const string query = """
                             SELECT id AS Id, name AS Name, project_id AS ProjectId
                             FROM boards
                             WHERE project_id = @ProjectId
                             """;

        IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Board>(query, new { ProjectId = projectId });
    }
}

public record Board(string Id, string Name, string ProjectId);