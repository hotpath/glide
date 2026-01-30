using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Sessions;

public class SessionRepository(IDbConnectionFactory connectionFactory) : ISessionRepository
{
    public async Task<Session> CreateAsync(string userId, long durationSeconds)
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

    public async Task<SessionUser?> GetAsync(string sessionId)
    {
        const string query = """
                             SELECT s.id, s.user_id AS UserId, s.expires_at, u.email, u.display_name AS DisplayName
                             FROM sessions s
                             JOIN users u ON s.user_id = u.id
                             WHERE s.id = @SessionId AND s.expires_at > @Now
                             """;
        IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SessionUser>(query,
            new { SessionId = sessionId, Now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
    }

    public async Task DeleteAsync(string sessionId)
    {
        const string statement = "DELETE FROM sessions WHERE id = @SessionId";
        IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { SessionId = sessionId });
    }
}

public record SessionUser
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public long ExpiresAt { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
}

public record Session(string Id, string UserId, long CreatedAt, long ExpiresAt);