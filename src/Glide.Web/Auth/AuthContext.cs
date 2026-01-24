using System.Collections.Generic;
using System.Threading;

namespace Glide.Web.Auth;

public class AuthContext(ForgejoOAuthConfig forgejoOAuthConfig)
{
    public HashSet<string> States { get; } = [];
    public ForgejoOAuthConfig ForgejoOAuthConfig { get; init; } = forgejoOAuthConfig;
    public Lock StateLock { get; } = new();
}