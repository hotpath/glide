using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.UserOAuthProviders;

public class UserOAuthProviderRepository(IDbConnectionFactory connectionFactory) : IUserOAuthProviderRepository
{
    public async Task<UserOAuthProvider?> GetByProviderAndProviderUserIdAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        const string query = """
                             SELECT * FROM user_oauth_providers
                             WHERE provider = @Provider
                             AND provider_user_id = @ProviderUserId
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserOAuthProvider>(query,
            new { Provider = provider, ProviderUserId = providerUserId });
    }

    public async Task<IEnumerable<UserOAuthProvider>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        const string query = """
                             SELECT * FROM user_oauth_providers
                             WHERE user_id = @UserId
                             ORDER BY created_at ASC
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<UserOAuthProvider>(query, new { UserId = userId });
    }

    public async Task<UserOAuthProvider> CreateAsync(UserOAuthProvider provider)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();

        const string insertStatement = """
                                       INSERT INTO user_oauth_providers
                                       (id, user_id, provider, provider_user_id, provider_email, created_at, updated_at)
                                       VALUES
                                       (@Id, @UserId, @Provider, @ProviderUserId, @ProviderEmail, @CreatedAt, @UpdatedAt)
                                       """;

        await conn.ExecuteAsync(insertStatement, provider);
        return provider;
    }

    public async Task DeleteAsync(string id)
    {
        using IDbConnection conn = connectionFactory.CreateConnection();

        const string deleteStatement = "DELETE FROM user_oauth_providers WHERE id = @Id";

        await conn.ExecuteAsync(deleteStatement, new { Id = id });
    }
}