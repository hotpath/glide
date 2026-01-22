using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Glide.Web.Auth;

public class OAuthClient(IHttpClientFactory clientFactory, ForgejoOAuthConfig authConfig, ILogger<OAuthClient> logger)
{
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };


    public async Task<string?> ExchangeCodeForToken(string code, CancellationToken cancellationToken = default)
    {
        HttpClient client = clientFactory.CreateClient("ForgejoOAuth");

        Dictionary<string, string> formData = new()
        {
            { "grant_type", "authorization_code" },
            { "client_id", authConfig.ClientId },
            { "client_secret", authConfig.ClientSecret },
            { "code", code },
            { "redirect_uri", authConfig.RedirectUri.ToString() }
        };

        FormUrlEncodedContent content = new(formData);

        HttpResponseMessage response = await client.PostAsync("/login/oauth/access_token", content, cancellationToken);

        response.EnsureSuccessStatusCode();

        TokenResult? responseBody = await response.Content.ReadFromJsonAsync<TokenResult>(_jsonOpts, cancellationToken);
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("OAuth Login Response: {}", responseBody);
        }

        return responseBody?.AccessToken;
    }

    public async Task<ForgejoUser?> GetUserInfo(string accessToken, CancellationToken cancellationToken = default)
    {
        HttpClient client = clientFactory.CreateClient("ForgejoOAuth");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);

        HttpResponseMessage response = await client.GetAsync("/api/v1/user");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ForgejoUser>(_jsonOpts, cancellationToken);
    }
}

public record ForgejoUser
{
    public long Id { get; init; }
    public required string Login { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
}

public class TokenResult
{
    public string? AccessToken { get; init; }

    public override string ToString()
    {
        return AccessToken ?? "unknown";
    }
}