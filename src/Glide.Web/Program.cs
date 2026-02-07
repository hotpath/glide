using System;
using System.Collections.Generic;
using System.Net;

using Dapper;

using DotNetEnv;

using FluentMigrator.Runner;

using Glide.Data;
using Glide.Data.Boards;
using Glide.Data.Cards;
using Glide.Data.Columns;
using Glide.Data.Labels;
using Glide.Data.Migrations;
using Glide.Data.Sessions;
using Glide.Data.SiteSettings;
using Glide.Data.UserOAuthProviders;
using Glide.Data.Users;
using Glide.Web.App;
using Glide.Web.Auth;
using Glide.Web.Boards;
using Glide.Web.Cards;
using Glide.Web.Columns;
using Glide.Web.Features;
using Glide.Web.Labels;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Env.Load();

// Configure Dapper to honor snake_case columns
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Register Dapper type handlers for DateOnly
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Validate required configuration early
ConfigurationValidator.ValidateRequired(builder.Configuration);

builder.Services.AddRazorComponents();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.WebHost.ConfigureKestrel(options =>
{
    string port = Environment.GetEnvironmentVariable("GLIDE_PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// Provider configurations
builder.Services.AddSingleton(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    return new Dictionary<string, OAuthProviderConfig>
    {
        ["github"] = OAuthProviderConfig.FromConfig(config, "GITHUB")
    };
});

// Update AuthContext to use new config dictionary
builder.Services.AddSingleton<AuthContext>(sp =>
{
    Dictionary<string, OAuthProviderConfig> configs = sp.GetRequiredService<Dictionary<string, OAuthProviderConfig>>();
    return new AuthContext(configs);
});

// Register session configuration (applies to all auth methods)
builder.Services.AddSingleton(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    return SessionConfig.FromConfiguration(config);
});

// Register admin email configuration
builder.Services.AddSingleton(sp =>
{
    string? adminEmail = Environment.GetEnvironmentVariable("GLIDE_ADMIN_EMAIL");
    return new AdminConfig { AdminEmail = adminEmail };
});

// Register provider implementations
builder.Services.AddSingleton<IOAuthProvider, GitHubOAuthProvider>();

// Register factory
builder.Services.AddSingleton<OAuthProviderFactory>();

// Register password authentication service
builder.Services.AddSingleton<PasswordAuthService>();

// HTTP clients for each provider
builder.Services.AddHttpClient("githubOAuth", (sp, client) =>
{
    Dictionary<string, OAuthProviderConfig> configs = sp.GetRequiredService<Dictionary<string, OAuthProviderConfig>>();
    client.BaseAddress = configs["github"].BaseUri;
});

// DB Migrations
string dbPath = Environment.GetEnvironmentVariable("GLIDE_DATABASE_PATH") ?? "/app/glide.db";

builder.Services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory($"Data Source={dbPath}"))
    .AddSingleton<IUserRepository, UserRepository>()
    .AddSingleton<IUserOAuthProviderRepository, UserOAuthProviderRepository>()
    .AddSingleton<ISessionRepository, SessionRepository>()
    .AddSingleton<IBoardRepository, BoardRepository>()
    .AddSingleton<IColumnRepository, ColumnRepository>()
    .AddSingleton<ICardRepository, CardRepository>()
    .AddSingleton<ILabelRepository, LabelRepository>()
    .AddSingleton<ISiteSettingsRepository, SiteSettingsRepository>();

builder.Services.AddSingleton<AuthAction>()
    .AddSingleton<BoardAction>()
    .AddSingleton<CardAction>()
    .AddSingleton<ColumnAction>()
    .AddSingleton<LabelAction>()
    .AddSingleton<SettingsAction>()
    .AddSingleton<DateFormatService>();

builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString($"Data Source={dbPath}")
        .ScanIn(typeof(CreateInitialSchema).Assembly).For.All())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Configure data protection to persist keys (important for containers/restarts)
string dataProtectionPath = Environment.GetEnvironmentVariable("GLIDE_DATA_PROTECTION_PATH") ?? "/app/keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Glide");

// Configure forwarded headers for reverse proxy support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".AspNetCore.Glide.Auth";
        options.Cookie.HttpOnly = true;
        // Always use Secure cookies when not in development to work properly behind reverse proxies
        options.Cookie.SecurePolicy = builder.Environment.EnvironmentName == "Development"
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        // Allow explicit domain override via environment variable for Cloudflare Tunnels
        string? cookieDomain = Environment.GetEnvironmentVariable("GLIDE_COOKIE_DOMAIN");
        if (!string.IsNullOrWhiteSpace(cookieDomain))
        {
            options.Cookie.Domain = cookieDomain;
        }

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

// Enable forwarded headers for reverse proxy support
app.UseForwardedHeaders();

app.UseStaticFiles();

app.UseMiddleware<SessionValidationMiddleware>();

// UseAuthentication is not needed since we use custom SessionValidationMiddleware
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => new RazorComponentResult<Home>());

app.MapGet("/dashboard", () => new RazorComponentResult<Dashboard>()).RequireAuthorization();
await app.RunAsync();