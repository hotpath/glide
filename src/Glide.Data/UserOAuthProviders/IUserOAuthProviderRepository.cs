using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glide.Data.UserOAuthProviders;

public interface IUserOAuthProviderRepository
{
    Task<UserOAuthProvider?> GetByProviderAndProviderUserIdAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<UserOAuthProvider>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserOAuthProvider> CreateAsync(UserOAuthProvider provider);

    Task DeleteAsync(string id);
}