using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Glide.Web.Auth;

public class ForgejoOAuthProvider : IOAuthProvider
{
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public string Name => "forgejo";
    public string DisplayName => "Codeberg";

    public Uri GetAuthorizeUrl(string clientId, Uri redirectUri, string state, Uri baseUri)
    {
        UriBuilder builder = new(baseUri)
        {
            Path = "/login/oauth/authorize",
            Query = $"client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri.ToString())}&response_type=code&state={Uri.EscapeDataString(state)}"
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
            { "grant_type", "authorization_code" },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code },
            { "redirect_uri", redirectUri.ToString() }
        };

        FormUrlEncodedContent content = new(formData);

        HttpResponseMessage response = await client.PostAsync("/login/oauth/access_token", content, cancellationToken);

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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);

        HttpResponseMessage response = await client.GetAsync("/api/v1/user", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        ForgejoUser? user = await response.Content.ReadFromJsonAsync<ForgejoUser>(_jsonOpts, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new OAuthUserInfo(
            ProviderId: user.Id.ToString(),
            Username: user.Login,
            Email: user.Email,
            DisplayName: user.FullName
        );
    }

    private record ForgejoUser
    {
        public long Id { get; init; }
        public required string Login { get; init; }
        public required string Email { get; init; }
        public required string FullName { get; init; }
    }

    private record TokenResult
    {
        public string? AccessToken { get; init; }
    }
}
