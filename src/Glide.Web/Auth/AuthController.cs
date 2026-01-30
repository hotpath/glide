using System;
using System.Threading;
using System.Threading.Tasks;

using Glide.Data.Sessions;
using Glide.Data.Users;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Glide.Web.Auth;

[Route("auth")]
[ApiController]
public class AuthController(
    AuthContext authContext,
    OAuthClient oAuthClient,
    ILogger<AuthController> logger,
    IUserRepository userRepository,
    ISessionRepository sessionRepository) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login()
    {
        string state = Csrf.GenerateState();

        using (authContext.StateLock.EnterScope())
        {
            authContext.States.Add(state);
        }

        Uri authBase = new(authContext.ForgejoOAuthConfig.BaseUri, "/login/oauth/authorize");

        string authUrl =
            $"{authBase}?client_id={authContext.ForgejoOAuthConfig.ClientId}&redirect_uri={authContext.ForgejoOAuthConfig.RedirectUri}&response_type=code&state={state}";

        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken = default)
    {
        logger.LogTrace("Validating CSRF state {}", state);

        bool stateValid;
        using (authContext.StateLock.EnterScope())
        {
            stateValid = authContext.States.Remove(state);
        }

        if (!stateValid)
        {
            logger.LogDebug("State {} is not valid", state);

            return Forbid();
        }

        string? token = await oAuthClient.ExchangeCodeForToken(code, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogDebug("Failed to exchange authorization code {} for token", code);

            return Forbid();
        }

        ForgejoUser? oAuthUser = await oAuthClient.GetUserInfo(token, cancellationToken);

        if (oAuthUser is null)
        {
            logger.LogDebug("Failed to fetch user information for code {code}", code);

            return Forbid();
        }

        logger.LogTrace("Retrieved user {oAuthUser}", oAuthUser);

        User user = await userRepository.CreateOrUpdateFromOAuthAsync("forgejo", oAuthUser.Id.ToString(),
            oAuthUser.FullName,
            oAuthUser.Email);

        logger.LogTrace("Created or updated user {user}", user);

        Session session =
            await sessionRepository.CreateAsync(user.Id, authContext.ForgejoOAuthConfig.SessionDurationSeconds);

        logger.LogTrace("Session created: {session}", session.Id);

        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(authContext.ForgejoOAuthConfig.SessionDurationSeconds),
                Path = "/"
            });

        return Redirect("/dashboard");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string? sessionId = HttpContext.Request.Cookies["glide_session"];

        if (!string.IsNullOrEmpty(sessionId))
        {
            await sessionRepository.DeleteAsync(sessionId);
        }

        HttpContext.Response.Cookies.Delete("glide_session");
        return Redirect("/");
    }
}