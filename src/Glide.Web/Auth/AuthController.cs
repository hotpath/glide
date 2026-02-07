using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Glide.Data.Sessions;
using Glide.Data.Users;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Glide.Web.Auth;

[Route("auth")]
[ApiController]
public class AuthController(
    AuthContext authContext,
    OAuthProviderFactory providerFactory,
    IHttpClientFactory clientFactory,
    SessionConfig sessionConfig,
    ILogger<AuthController> logger,
    AuthAction authAction) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string provider = "github")
    {
        string state = Csrf.GenerateState();

        using (authContext.StateLock.EnterScope())
        {
            authContext.States.Add(state);
            authContext.StateToProvider[state] = provider;
        }

        IOAuthProvider oauthProvider = providerFactory.GetProvider(provider);
        OAuthProviderConfig config = providerFactory.GetConfig(provider);

        Uri authUrl = oauthProvider.GetAuthorizeUrl(config.ClientId, config.RedirectUri, state, config.BaseUri);
        return Redirect(authUrl.ToString());
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken = default)
    {
        logger.LogTrace("Validating CSRF state {}", state);

        string? providerFromState;
        bool stateValid;

        using (authContext.StateLock.EnterScope())
        {
            stateValid = authContext.States.Remove(state);
            authContext.StateToProvider.TryGetValue(state, out providerFromState);
            authContext.StateToProvider.Remove(state);
        }

        string provider = providerFromState ?? "github";

        if (!stateValid)
        {
            logger.LogDebug("State {} is not valid", state);
            return Forbid();
        }

        IOAuthProvider oauthProvider = providerFactory.GetProvider(provider);
        OAuthProviderConfig config = providerFactory.GetConfig(provider);
        HttpClient client = clientFactory.CreateClient($"{provider}OAuth");

        string? token = await oauthProvider.ExchangeCodeForTokenAsync(
            client, code, config.ClientId, config.ClientSecret, config.RedirectUri, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogDebug("Failed to exchange authorization code {} for token", code);
            return Forbid();
        }

        OAuthUserInfo? oauthUser = await oauthProvider.GetUserInfoAsync(client, token, cancellationToken);
        if (oauthUser is null)
        {
            logger.LogDebug("Failed to fetch user information for code {code}", code);
            return Forbid();
        }

        logger.LogTrace("Retrieved user {oauthUser}", oauthUser);

        AuthAction.Result<User> result =
            await authAction.HandleOAuthCallbackAsync(oauthUser, provider, cancellationToken);
        if (result.IsError)
        {
            return (IActionResult)result.StatusResult!;
        }

        Session session = await authAction.CreateSessionAsync(result.Object!.Id, config.SessionDurationSeconds);

        logger.LogTrace("Session created: {session}", session.Id);

        // Set custom session cookie
        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(config.SessionDurationSeconds),
                Path = "/"
            });

        // Sign in through ASP.NET Core authentication to avoid redirect loops
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, result.Object!.Id),
            new(ClaimTypes.Email, result.Object!.Email),
            new("SessionId", session.Id),
            new("DisplayName", result.Object!.DisplayName ?? result.Object!.Email),
            new("is_admin", result.Object!.IsAdmin.ToString())
        ];

        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(config.SessionDurationSeconds)
            });

        return Redirect("/dashboard");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string? sessionId = HttpContext.Request.Cookies["glide_session"];

        if (!string.IsNullOrEmpty(sessionId))
        {
            await authAction.DeleteSessionAsync(sessionId);
        }

        HttpContext.Response.Cookies.Delete("glide_session");

        // Sign out from ASP.NET Core authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? displayName = null)
    {
        AuthAction.Result<User> result = await authAction.RegisterAsync(email, password, displayName);
        if (result.IsError)
        {
            return (IActionResult)result.StatusResult!;
        }

        Session session = await authAction.CreateSessionAsync(result.Object!.Id, sessionConfig.DurationSeconds);
        logger.LogTrace("Session created: {sessionId}", session.Id);

        // Set custom session cookie
        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(sessionConfig.DurationSeconds),
                Path = "/"
            });

        // Sign in through ASP.NET Core authentication
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, result.Object!.Id),
            new(ClaimTypes.Email, result.Object!.Email),
            new("SessionId", session.Id),
            new("DisplayName", result.Object!.DisplayName ?? result.Object!.Email),
            new("is_admin", result.Object!.IsAdmin.ToString())
        ];

        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(sessionConfig.DurationSeconds)
            });

        return Redirect("/dashboard");
    }

    [HttpPost("login-password")]
    public async Task<IActionResult> LoginWithPassword(
        [FromForm] string email,
        [FromForm] string password)
    {
        AuthAction.Result<User> result = await authAction.LoginWithPasswordAsync(email, password);
        if (result.IsError)
        {
            return (IActionResult)result.StatusResult!;
        }

        Session session = await authAction.CreateSessionAsync(result.Object!.Id, sessionConfig.DurationSeconds);
        logger.LogTrace("Session created: {sessionId}", session.Id);

        // Set custom session cookie
        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(sessionConfig.DurationSeconds),
                Path = "/"
            });

        // Sign in through ASP.NET Core authentication
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, result.Object!.Id),
            new(ClaimTypes.Email, result.Object!.Email),
            new("SessionId", session.Id),
            new("DisplayName", result.Object!.DisplayName ?? result.Object!.Email),
            new("is_admin", result.Object!.IsAdmin.ToString())
        ];

        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(sessionConfig.DurationSeconds)
            });

        return Redirect("/dashboard");
    }
}