using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Glide.Web.Auth;

public interface IOAuthProvider
{
    string Name { get; }           // "github"
    string DisplayName { get; }    // "Codeberg" or "GitHub"

    Uri GetAuthorizeUrl(string clientId, Uri redirectUri, string state, Uri baseUri);
    Task<string?> ExchangeCodeForTokenAsync(
        HttpClient client,
        string code,
        string clientId,
        string clientSecret,
        Uri redirectUri,
        CancellationToken cancellationToken = default);
    Task<OAuthUserInfo?> GetUserInfoAsync(
        HttpClient client,
        string accessToken,
        CancellationToken cancellationToken = default);
}