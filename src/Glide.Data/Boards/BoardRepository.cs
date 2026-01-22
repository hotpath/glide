using System;
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
}

public record Board(string Id, string Name, string ProjectId);