using System;

using Microsoft.Extensions.Configuration;

namespace Glide.Web.Auth;

public record ForgejoOAuthConfig(
    string ClientId,
    string ClientSecret,
    Uri RedirectUri,
    Uri BaseUri)
{
    public static ForgejoOAuthConfig FromConfig(IConfiguration config)
    {
        string clientId = config["FORGEJO_CLIENT_ID"] ?? "";
        string clientSecret = config["FORGEJO_CLIENT_SECRET"] ?? "";
        Uri redirectUri = config.GetValue<Uri>("FORGEJO_REDIRECT_URI") ??
                          new Uri("http://localhost:8080/auth/callback");
        Uri baseUri = config.GetValue<Uri>("FORGEJO_BASE_URI") ?? new Uri("https://codeberg.org");

        return new ForgejoOAuthConfig(clientId, clientSecret, redirectUri, baseUri);
    }
}