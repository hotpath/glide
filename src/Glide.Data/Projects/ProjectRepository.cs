using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Projects;

public class ProjectRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<IEnumerable<Project>> GetByUserIdAsync(string userId)
    {
        const string query = """
                             SELECT p.id,p.name
                             FROM projects p
                             JOIN projects_users pu ON p.id = pu.project_id
                             WHERE pu.user_id = @UserId
                             """;
        IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Project>(query, new { UserId = userId });
    }

    public async Task<Project> CreateAsync(string name, string userId)
    {
        string newId = Guid.CreateVersion7().ToString();
        const string statement = "INSERT INTO projects(id,name) VALUES(@Id, @Name)";

        IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = newId, Name = name });


        const string relationshipStatement =
            "INSERT INTO projects_users(project_id, user_id) VALUES(@ProjectId,@UserId)";

        await conn.ExecuteAsync(relationshipStatement, new { ProjectId = newId, UserId = userId });

        return new Project(newId, name);
    }
}

public record Project(string Id, string Name);