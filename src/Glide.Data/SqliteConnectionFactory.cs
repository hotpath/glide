using System.Data;

using Microsoft.Data.Sqlite;

namespace Glide.Data;

public class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(connectionString);
    }
}