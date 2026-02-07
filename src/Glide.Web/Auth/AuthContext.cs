using System.Collections.Generic;
using System.Threading;

namespace Glide.Web.Auth;

public class AuthContext
{
    public AuthContext(Dictionary<string, OAuthProviderConfig> providerConfigs)
    {
        ProviderConfigs = providerConfigs;
    }

    public HashSet<string> States { get; } = [];
    public Dictionary<string, string> StateToProvider { get; } = new(); // state -> provider
    public Dictionary<string, OAuthProviderConfig> ProviderConfigs { get; }
    public Lock StateLock { get; } = new();
}