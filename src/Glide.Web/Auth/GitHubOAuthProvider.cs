using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Glide.Web.Auth;

public class GitHubOAuthProvider : IOAuthProvider
{
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public string Name => "github";
    public string DisplayName => "GitHub";

    public Uri GetAuthorizeUrl(string clientId, Uri redirectUri, string state, Uri baseUri)
    {
        string query = $"client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri.ToString())}&state={Uri.EscapeDataString(state)}&scope=user:email";
        UriBuilder builder = new(baseUri)
        {
            Path = "/login/oauth/authorize",
            Query = query
        };
        return builder.Uri;
    }

    public async Task<string?> ExchangeCodeForTokenAsync(
        HttpClient client,
        string code,
        string clientId,
        string clientSecret,
        Uri redirectUri,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> formData = new()
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code },
            { "redirect_uri", redirectUri.ToString() }
        };

        FormUrlEncodedContent content = new(formData);

        HttpRequestMessage request = new(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = content
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        TokenResult? responseBody = await response.Content.ReadFromJsonAsync<TokenResult>(_jsonOpts, cancellationToken);
        return responseBody?.AccessToken;
    }

    public async Task<OAuthUserInfo?> GetUserInfoAsync(
        HttpClient client,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Glide", "1.0"));

        HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        GitHubUser? user = await response.Content.ReadFromJsonAsync<GitHubUser>(_jsonOpts, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new OAuthUserInfo(
            ProviderId: user.Id.ToString(),
            Username: user.Login,
            Email: user.Email ?? "",
            DisplayName: user.Name ?? user.Login
        );
    }

    private record GitHubUser
    {
        public long Id { get; init; }
        public required string Login { get; init; }
        public string? Email { get; init; }
        public string? Name { get; init; }
    }

    private record TokenResult
    {
        public string? AccessToken { get; init; }
    }
}
