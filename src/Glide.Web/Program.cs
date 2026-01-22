using System;
using System.Threading;

using DotNetEnv;

using Glide.Web.Auth;
using Glide.Web.Features;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Env.Load();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddRazorComponents();

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.WebHost.ConfigureKestrel(options =>
{
    string port = Environment.GetEnvironmentVariable("GLIDE_PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

builder.Services.AddSingleton<ForgejoOAuthConfig>(sp =>
    ForgejoOAuthConfig.FromConfig(sp.GetRequiredService<IConfiguration>()));

builder.Services.AddSingleton<AuthContext>()
    .AddSingleton<OAuthClient>();

builder.Services.AddHttpClient("ForgejoOAuth", (sp, client) =>
{
    ForgejoOAuthConfig config = sp.GetRequiredService<ForgejoOAuthConfig>();
    client.BaseAddress = config.BaseUri;
});

WebApplication app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () => new RazorComponentResult<Home>());

app.MapGet("/auth/login", ([FromServices] AuthContext authContext) =>
{
    string state = Csrf.GenerateState();

    using (authContext.StateLock.EnterScope())
    {
        authContext.States.Add(state);
    }

    Uri authBase = new(authContext.ForgejoOAuthConfig.BaseUri, "/login/oauth/authorize");

    string authUrl =
        $"{authBase}?client_id={authContext.ForgejoOAuthConfig.ClientId}&redirect_uri={authContext.ForgejoOAuthConfig.RedirectUri}&response_type=code&state={state}";

    return Results.Redirect(authUrl);
});

app.MapGet("/auth/callback", async (
    [FromServices] OAuthClient oAuthClient,
    [FromServices] AuthContext authContext,
    [FromServices] ILogger<Program> logger,
    [FromQuery] string code,
    [FromQuery] string state,
    CancellationToken cancellationToken = default) =>
{
    if (logger.IsEnabled(LogLevel.Trace))
    {
        logger.LogTrace("Validating CSRF state {}", state);
    }

    bool stateValid = false;
    using (authContext.StateLock.EnterScope())
    {
        stateValid = authContext.States.Remove(state);
    }

    if (!stateValid)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("State {} is not valid", state);
        }

        return Results.Forbid();
    }

    string? token = await oAuthClient.ExchangeCodeForToken(code, cancellationToken);

    if (string.IsNullOrWhiteSpace(token))
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Failed to exchange authorization code {} for token", code);
        }

        return Results.Forbid();
    }

    ForgejoUser? user = await oAuthClient.GetUserInfo(token, cancellationToken);

    if (user is null)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Failed to fetch user information");
        }

        return Results.Forbid();
    }


    return Results.Ok(user.Email);
});
await app.RunAsync();