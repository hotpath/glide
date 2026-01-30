using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Users;

public class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<User?> GetAsync(string provider, string providerId, CancellationToken cancellationToken = default)
    {
        const string query = """
                             SELECT * FROM users
                             WHERE oauth_provider = @Provider
                             AND oauth_provider_id = @ProviderId
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(query,
            new { Provider = provider, ProviderId = providerId });
    }

    public async Task Create(User user)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();

        string insertStatement = """
                                 INSERT INTO users (id,display_name,email,oauth_provider,oauth_provider_id,created_at,updated_at)
                                 VALUES (@Id,@DisplayName,@Email,@OAuthProvider,@OAuthProviderId,@CreatedAt,@UpdatedAt);
                                 """;

        await conn.ExecuteAsync(insertStatement, user);
    }

    public async Task UpdateAsync(User user)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();

        string updateStatement = """
                                 UPDATE users
                                 SET display_name = @DisplayName, email = @Email, updated_at = @UpdatedAt
                                 """;

        User updatedUser = user with { UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

        await conn.ExecuteAsync(updateStatement, updatedUser);
    }

    public async Task<User> CreateOrUpdateFromOAuthAsync(string provider, string providerId, string displayName,
        string email)
    {
        // check if user exists by oauth id
        User? existingUser = await GetAsync(provider, providerId);

        if (existingUser == null)
        {
            long created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // create new
            User newUser = new()
            {
                Id = Guid.CreateVersion7().ToString(),
                DisplayName = displayName,
                Email = email,
                OAuthProvider = provider,
                OAuthProviderId = providerId,
                CreatedAt = created,
                UpdatedAt = created
            };

            await Create(newUser);

            return newUser;
        }

        // update existing
        User updatedUser = existingUser with { DisplayName = displayName, Email = email };

        await UpdateAsync(updatedUser);

        return updatedUser;
    }
}