using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Sessions;

public class SessionRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<Session> Create(string userId, long durationSeconds)
    {
        string sessionId = Guid.CreateVersion7().ToString();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAt = now.AddSeconds(durationSeconds);

        const string insertStatement = """
                                       INSERT INTO sessions(id, user_id, created_at, expires_at)
                                       VALUES (@Id, @UserId, @CreatedAt, @ExpiresAt);
                                       """;

        Session session = new(sessionId, userId, now.ToUnixTimeMilliseconds(), expiresAt.ToUnixTimeMilliseconds());

        IDbConnection conn = connectionFactory.CreateConnection();

        await conn.ExecuteAsync(insertStatement, session);

        return session;
    }
}

public record Session(string Id, string UserId, long CreatedAt, long ExpiresAt);