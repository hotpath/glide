using System.Threading;
using System.Threading.Tasks;

namespace Glide.Data.Users;

public interface IUserRepository
{
    Task<User?> GetAsync(string provider, string providerId, CancellationToken cancellationToken = default);
    Task Create(User user);
    Task UpdateAsync(User user);

    Task<User> CreateOrUpdateFromOAuthAsync(string provider, string providerId, string displayName,
        string email);
}