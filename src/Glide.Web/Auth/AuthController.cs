using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Glide.Data.Sessions;
using Glide.Data.Users;
using Glide.Data.UserOAuthProviders;

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
    PasswordAuthService passwordAuthService,
    SessionConfig sessionConfig,
    ILogger<AuthController> logger,
    IUserRepository userRepository,
    IUserOAuthProviderRepository oauthProviderRepository,
    ISessionRepository sessionRepository) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string provider = "forgejo")
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

        string provider = providerFromState ?? "forgejo";

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

        // Check if OAuth provider link already exists
        UserOAuthProvider? oauthProviderRecord = await oauthProviderRepository.GetByProviderAndProviderUserIdAsync(
            provider, oauthUser.ProviderId, cancellationToken);

        User user;
        if (oauthProviderRecord is not null)
        {
            // User has logged in with this provider before
            logger.LogTrace("Found existing OAuth provider record for user {userId}", oauthProviderRecord.UserId);
            User? existingUser = await userRepository.GetByIdAsync(oauthProviderRecord.UserId);
            
            if (existingUser is not null)
            {
                user = existingUser;

                // Update user info if display name changed
                if (user.DisplayName != oauthUser.DisplayName)
                {
                    await userRepository.UpdateAsync(user with { DisplayName = oauthUser.DisplayName });
                    user = user with { DisplayName = oauthUser.DisplayName };
                }
            }
            else
            {
                // Orphaned OAuth provider record (user was deleted). Clean it up and link to existing user if available.
                logger.LogDebug("OAuth provider record found but user missing. Deleting orphaned record.");
                await oauthProviderRepository.DeleteAsync(oauthProviderRecord.Id);

                // Check if user exists by email and link provider to them
                User? userByEmail = await userRepository.GetByEmailAsync(oauthUser.Email);

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (userByEmail is not null)
                {
                    logger.LogTrace("Linking OAuth provider to existing user {userId}", userByEmail.Id);
                    user = userByEmail;

                    UserOAuthProvider newProviderLink = new()
                    {
                        Id = Guid.CreateVersion7().ToString(),
                        UserId = user.Id,
                        Provider = provider,
                        ProviderUserId = oauthUser.ProviderId,
                        ProviderEmail = oauthUser.Email,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await oauthProviderRepository.CreateAsync(newProviderLink);

                    // Update display name if different
                    if (user.DisplayName != oauthUser.DisplayName)
                    {
                        await userRepository.UpdateAsync(user with { DisplayName = oauthUser.DisplayName });
                        user = user with { DisplayName = oauthUser.DisplayName };
                    }
                }
                else
                {
                    // Create new user
                    logger.LogTrace("Creating new user for OAuth provider {provider}", provider);
                    user = new User
                    {
                        Id = Guid.CreateVersion7().ToString(),
                        DisplayName = oauthUser.DisplayName,
                        Email = oauthUser.Email,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await userRepository.CreateAsync(user);

                    UserOAuthProvider newProviderLink = new()
                    {
                        Id = Guid.CreateVersion7().ToString(),
                        UserId = user.Id,
                        Provider = provider,
                        ProviderUserId = oauthUser.ProviderId,
                        ProviderEmail = oauthUser.Email,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await oauthProviderRepository.CreateAsync(newProviderLink);
                }
            }
        }
        else
        {
            // Check if user exists by email (for account linking)
            User? existingUser = await userRepository.GetByEmailAsync(oauthUser.Email);

            if (existingUser is not null)
            {
                // Link new OAuth provider to existing user account
                logger.LogTrace("Linking OAuth provider to existing user {userId}", existingUser.Id);
                user = existingUser;

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                UserOAuthProvider newProviderLink = new()
                {
                    Id = Guid.CreateVersion7().ToString(),
                    UserId = user.Id,
                    Provider = provider,
                    ProviderUserId = oauthUser.ProviderId,
                    ProviderEmail = oauthUser.Email,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await oauthProviderRepository.CreateAsync(newProviderLink);

                // Update display name if different
                if (user.DisplayName != oauthUser.DisplayName)
                {
                    await userRepository.UpdateAsync(user with { DisplayName = oauthUser.DisplayName });
                    user = user with { DisplayName = oauthUser.DisplayName };
                }
            }
            else
            {
                // Create new user and OAuth provider link
                logger.LogTrace("Creating new user for OAuth provider {provider}", provider);

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                user = new User
                {
                    Id = Guid.CreateVersion7().ToString(),
                    DisplayName = oauthUser.DisplayName,
                    Email = oauthUser.Email,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await userRepository.CreateAsync(user);

                UserOAuthProvider newProviderLink = new()
                {
                    Id = Guid.CreateVersion7().ToString(),
                    UserId = user.Id,
                    Provider = provider,
                    ProviderUserId = oauthUser.ProviderId,
                    ProviderEmail = oauthUser.Email,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await oauthProviderRepository.CreateAsync(newProviderLink);
            }
        }

        logger.LogTrace("Authenticated user {user}", user);

        Session session = await sessionRepository.CreateAsync(user.Id, config.SessionDurationSeconds);

        logger.LogTrace("Session created: {session}", session.Id);

        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(config.SessionDurationSeconds),
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

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? displayName = null)
    {
        logger.LogTrace("Registration attempt for email: {email}", email);

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }

        // Validate password strength
        string? passwordError = passwordAuthService.ValidatePasswordStrength(password);
        if (passwordError is not null)
        {
            return BadRequest(passwordError);
        }

        // Check if user already exists
        User? existingUser = await userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            return BadRequest("An account with this email already exists");
        }

        // Hash the password
        string passwordHash = passwordAuthService.HashPassword(password);

        // Create new user
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User newUser = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            Email = email,
            DisplayName = displayName ?? email.Split('@')[0], // Use email prefix as default display name
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };

        await userRepository.CreateAsync(newUser);
        logger.LogTrace("Created new user: {userId}", newUser.Id);

        // Create session
        Session session = await sessionRepository.CreateAsync(newUser.Id, sessionConfig.DurationSeconds);
        logger.LogTrace("Session created: {sessionId}", session.Id);

        // Set cookie
        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(sessionConfig.DurationSeconds),
                Path = "/"
            });

        return Redirect("/dashboard");
    }

    [HttpPost("login-password")]
    public async Task<IActionResult> LoginWithPassword(
        [FromForm] string email,
        [FromForm] string password)
    {
        logger.LogTrace("Password login attempt for email: {email}", email);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest("Email and password are required");
        }

        // Find user by email
        User? user = await userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            // Don't reveal whether user exists
            return BadRequest("Invalid email or password");
        }

        // Check if user has a password set
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest("This account uses OAuth login. Please use the OAuth buttons to log in.");
        }

        // Verify password
        bool passwordValid = passwordAuthService.VerifyPassword(password, user.PasswordHash);
        if (!passwordValid)
        {
            logger.LogDebug("Invalid password for user: {userId}", user.Id);
            return BadRequest("Invalid email or password");
        }

        logger.LogTrace("Password verified for user: {userId}", user.Id);

        // Create session
        Session session = await sessionRepository.CreateAsync(user.Id, sessionConfig.DurationSeconds);
        logger.LogTrace("Session created: {sessionId}", session.Id);

        // Set cookie
        HttpContext.Response.Cookies.Append("glide_session", session.Id,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(sessionConfig.DurationSeconds),
                Path = "/"
            });

        return Redirect("/dashboard");
    }
}