using System.Data;

namespace Glide.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}