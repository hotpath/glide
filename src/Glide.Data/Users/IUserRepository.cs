using System.Threading;
using System.Threading.Tasks;

namespace Glide.Data.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
}