using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glide.Data.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> SearchByEmailAsync(string emailQuery);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task SetAdminStatusAsync(string userId, bool isAdmin);
}