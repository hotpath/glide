using System;
using System.Collections.Generic;
using System.Linq;

namespace Glide.Web.Auth;

public class OAuthProviderFactory
{
    private readonly Dictionary<string, IOAuthProvider> _providers;
    private readonly Dictionary<string, OAuthProviderConfig> _configs;

    public OAuthProviderFactory(
        IEnumerable<IOAuthProvider> providers,
        Dictionary<string, OAuthProviderConfig> configs)
    {
        _providers = providers.ToDictionary(p => p.Name, p => p);
        _configs = configs;
    }

    public IOAuthProvider GetProvider(string name) =>
        _providers.TryGetValue(name, out var p) ? p :
        throw new ArgumentException($"Unknown provider: {name}");

    public OAuthProviderConfig GetConfig(string name) =>
        _configs.TryGetValue(name, out var c) ? c :
        throw new ArgumentException($"No config for provider: {name}");

    public IEnumerable<IOAuthProvider> GetAllProviders() => _providers.Values;
}
