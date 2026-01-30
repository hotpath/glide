using System;

using Dapper;

using DotNetEnv;

using FluentMigrator.Runner;

using Glide.Data;
using Glide.Data.Boards;
using Glide.Data.Migrations;
using Glide.Data.Sessions;
using Glide.Data.Swimlanes;
using Glide.Data.Tasks;
using Glide.Data.Users;
using Glide.Web.Auth;
using Glide.Web.Boards;
using Glide.Web.Features;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Env.Load();

// Configure Dapper to honor snake_case columns
DefaultTypeMap.MatchNamesWithUnderscores = true;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddRazorComponents();
builder.Services.AddControllers();
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
string dbPath = Environment.GetEnvironmentVariable("GLIDE_DATABASE_PATH") ?? "/app/glide.db";

builder.Services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory($"Data Source={dbPath}"))
    .AddSingleton<UserRepository>()
    .AddSingleton<SessionRepository>()
    .AddSingleton<BoardRepository>()
    .AddSingleton<SwimlaneRepository>()
    .AddSingleton<TaskRepository>();

builder.Services.AddSingleton<BoardAction>();

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

app.UseStaticFiles();

app.UseMiddleware<SessionValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => new RazorComponentResult<Home>());

app.MapGet("/dashboard", () => new RazorComponentResult<Dashboard>()).RequireAuthorization();
await app.RunAsync();