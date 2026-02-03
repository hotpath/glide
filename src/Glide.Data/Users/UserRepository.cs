using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Users;

public class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<User?> GetByIdAsync(string id)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE id = @Id",
            new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM users WHERE email = @Email",
            new { Email = email });
    }

    public async Task<IEnumerable<User>> SearchByEmailAsync(string emailQuery)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<User>(
            """
            SELECT * FROM users WHERE email LIKE @EmailQuery
            ORDER BY email ASC
            LIMIT 10
            """,
            new { EmailQuery = $"%{emailQuery}%" });
    }

    public async Task<User> CreateAsync(User user)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();

        const string insertStatement = """
                                       INSERT INTO users (id, display_name, email, password_hash, created_at, updated_at)
                                       VALUES (@Id, @DisplayName, @Email, @PasswordHash, @CreatedAt, @UpdatedAt)
                                       """;

        await conn.ExecuteAsync(insertStatement, user);
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();

        const string updateStatement = """
                                       UPDATE users
                                       SET display_name = @DisplayName,
                                           email = @Email,
                                           password_hash = @PasswordHash,
                                           updated_at = @UpdatedAt
                                       WHERE id = @Id
                                       """;

        User updatedUser = user with { UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

        await conn.ExecuteAsync(updateStatement, updatedUser);
    }
}