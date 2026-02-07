using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.SiteSettings;

public class SiteSettingsRepository(IDbConnectionFactory connectionFactory) : ISiteSettingsRepository
{
    public async Task<SiteSetting?> GetByKeyAsync(string key)
    {
        const string query = "SELECT key, value, created_at, updated_at FROM site_settings WHERE key = @Key";
        IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SiteSetting>(query, new { Key = key });
    }

    public async Task UpdateAsync(string key, string value)
    {
        const string statement = """
                                 UPDATE site_settings
                                 SET value = @Value, updated_at = @UpdatedAt
                                 WHERE key = @Key
                                 """;
        IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement,
            new { Key = key, Value = value, UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
    }
}