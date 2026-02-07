using System;
using System.Collections.Generic;
using System.Linq;

namespace Glide.Web.Auth;

public class OAuthProviderFactory
{
    private readonly Dictionary<string, OAuthProviderConfig> _configs;
    private readonly Dictionary<string, IOAuthProvider> _providers;

    public OAuthProviderFactory(
        IEnumerable<IOAuthProvider> providers,
        Dictionary<string, OAuthProviderConfig> configs)
    {
        _providers = providers.ToDictionary(p => p.Name, p => p);
        _configs = configs;
    }

    public IOAuthProvider GetProvider(string name)
    {
        return _providers.TryGetValue(name, out IOAuthProvider? p)
            ? p
            : throw new ArgumentException($"Unknown provider: {name}");
    }

    public OAuthProviderConfig GetConfig(string name)
    {
        return _configs.TryGetValue(name, out OAuthProviderConfig? c)
            ? c
            : throw new ArgumentException($"No config for provider: {name}");
    }

    public IEnumerable<IOAuthProvider> GetAllProviders()
    {
        return _providers.Values;
    }
}