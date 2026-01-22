using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

using DotNetEnv;

using FluentMigrator.Runner;

using Glide.Data;
using Glide.Data.Migrations;
using Glide.Data.Sessions;
using Glide.Data.Users;
using Glide.Web.Auth;
using Glide.Web.Features;

using Microsoft.AspNetCore.Authentication.Cookies;
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

// DB Migrations
string? dbPath = Environment.GetEnvironmentVariable("GLIDE_DATABASE_PATH") ?? "/app/glide.db";

builder.Services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory($"Data Source={dbPath}"));
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<SessionRepository>();

builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString($"Data Source={dbPath}")
        .ScanIn(typeof(CreateInitialSchema).Assembly).For.All())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "glide_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(720);
        options.SlidingExpiration = true;
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
    });
builder.Services.AddAuthorization();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    IMigrationRunner runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

// validate session on each request
app.Use(async (context, next) =>
{
    string? sessionId = context.Request.Cookies["glide_session"];

    if (!string.IsNullOrEmpty(sessionId))
    {
        SessionRepository sessionRepo = context.RequestServices.GetRequiredService<SessionRepository>();

        SessionUser? session = await sessionRepo.Get(sessionId);

        if (session is not null)
        {
            // set user identity
            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, session.UserId),
                new(ClaimTypes.Email, session.Email),
                new("SessionId", session.Id)
            ];

            ClaimsIdentity identity = new(claims, "DatabaseSession");
            context.User = new ClaimsPrincipal(identity);
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

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
    [FromServices] UserRepository userRepository,
    [FromServices] SessionRepository sessionRepository,
    [FromQuery] string code,
    [FromQuery] string state,
    HttpContext context,
    CancellationToken cancellationToken = default) =>
{
    logger.LogTrace("Validating CSRF state {}", state);

    bool stateValid = false;
    using (authContext.StateLock.EnterScope())
    {
        stateValid = authContext.States.Remove(state);
    }

    if (!stateValid)
    {
        logger.LogDebug("State {} is not valid", state);

        return Results.Forbid();
    }

    string? token = await oAuthClient.ExchangeCodeForToken(code, cancellationToken);

    if (string.IsNullOrWhiteSpace(token))
    {
        logger.LogDebug("Failed to exchange authorization code {} for token", code);

        return Results.Forbid();
    }

    ForgejoUser? oAuthUser = await oAuthClient.GetUserInfo(token, cancellationToken);

    if (oAuthUser is null)
    {
        logger.LogDebug("Failed to fetch user information for code {code}", code);

        return Results.Forbid();
    }

    logger.LogTrace("Retrieved user {oAuthUser}", oAuthUser);

    User user = await userRepository.CreateOrUpdateFromOAuth("forgejo", oAuthUser.Id.ToString(), oAuthUser.FullName,
        oAuthUser.Email);

    logger.LogTrace("Created or updated user {user}", user);

    Session session = await sessionRepository.Create(user.Id, authContext.ForgejoOAuthConfig.SessionDurationSeconds);

    logger.LogTrace("Session created: {session}", session.Id);

    context.Response.Cookies.Append("glide_session", session.Id,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromSeconds(authContext.ForgejoOAuthConfig.SessionDurationSeconds),
            Path = "/"
        });

    return Results.Redirect("/dashboard");
});

app.MapPost("/logout", async (HttpContext context, [FromServices] SessionRepository sessionRepo) =>
{
    string? sessionId = context.Request.Cookies["glide_session"];

    if (!string.IsNullOrEmpty(sessionId))
    {
        await sessionRepo.Delete(sessionId);
    }

    context.Response.Cookies.Delete("glide_session");
    return Results.Redirect("/");
});

app.MapGet("/dashboard", () => Results.Ok("dashboard")).RequireAuthorization();
await app.RunAsync();