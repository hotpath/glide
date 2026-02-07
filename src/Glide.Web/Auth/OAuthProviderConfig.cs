using System;

using Microsoft.Extensions.Configuration;

namespace Glide.Web.Auth;

public record OAuthProviderConfig(
    string ClientId,
    string ClientSecret,
    Uri RedirectUri,
    Uri BaseUri,
    long SessionDurationSeconds)
{
    public static OAuthProviderConfig FromConfig(IConfiguration config, string prefix)
    {
        string clientId = config[$"{prefix}_CLIENT_ID"] ?? "";
        string clientSecret = config[$"{prefix}_CLIENT_SECRET"] ?? "";
        Uri redirectUri = config.GetValue<Uri>($"{prefix}_REDIRECT_URI") ??
                          new Uri("http://localhost:8080/auth/callback");
        Uri baseUri = config.GetValue<Uri>($"{prefix}_BASE_URI") ??
                      new Uri(prefix == "GITHUB" ? "https://github.com" : "https://codeberg.org");

        long durationHours = config.GetValue<long>("SESSION_DURATION_HOURS", 720); // Default: 30 days
        long durationSeconds = (long)TimeSpan.FromHours(durationHours).TotalSeconds;

        return new OAuthProviderConfig(clientId, clientSecret, redirectUri, baseUri, durationSeconds);
    }
}