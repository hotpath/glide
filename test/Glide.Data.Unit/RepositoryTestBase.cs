using System.Data;

using Dapper;

using FluentMigrator.Runner;

using Glide.Data.Migrations;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Glide.Data.Unit;

public abstract class RepositoryTestBase : IDisposable, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _databasePath;
    private readonly ServiceProvider _serviceProvider;

    protected RepositoryTestBase()
    {
        // Configure Dapper to match snake_case
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Create temporary file-based SQLite database for test isolation
        _databasePath = Path.Combine(Path.GetTempPath(), $"glide_test_{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        // Set up FluentMigrator
        _serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connection.ConnectionString)
                .ScanIn(typeof(CreateInitialSchema).Assembly).For.All())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider();

        // Run migrations
        using IServiceScope scope = _serviceProvider.CreateScope();
        IMigrationRunner runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        // Create connection factory that uses the test database
        ConnectionFactory = new TestConnectionFactory(_connection.ConnectionString);
    }

    protected IDbConnectionFactory ConnectionFactory { get; }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        CleanupDatabase();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _serviceProvider.Dispose();
        CleanupDatabase();
    }

    private void CleanupDatabase()
    {
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    protected async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        using IDbConnection connection = ConnectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using IDbConnection connection = ConnectionFactory.CreateConnection();
        return await connection.QueryAsync<T>(sql, param);
    }

    protected async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using IDbConnection connection = ConnectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, param);
    }

    private class TestConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            return new SqliteConnection(connectionString);
        }
    }
}