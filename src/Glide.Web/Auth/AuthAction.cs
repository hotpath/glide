using System;
using System.Threading;
using System.Threading.Tasks;

using Glide.Data.Sessions;
using Glide.Data.UserOAuthProviders;
using Glide.Data.Users;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Glide.Web.Auth;

public class AuthAction(
    IUserRepository userRepository,
    IUserOAuthProviderRepository oauthProviderRepository,
    ISessionRepository sessionRepository,
    AdminConfig adminConfig,
    PasswordAuthService passwordAuthService,
    ILogger<AuthAction> logger)
{
    public record Result<T>(T? Object, IResult? StatusResult = null)
    {
        public Result(IResult statusResult) : this(default, statusResult) { }
        public bool IsError => StatusResult is not null;
    }

    public async Task<Result<User>> HandleOAuthCallbackAsync(
        OAuthUserInfo oauthUser,
        string provider,
        CancellationToken cancellationToken = default)
    {
        // Check if OAuth provider link already exists
        UserOAuthProvider? oauthProviderRecord = await oauthProviderRepository.GetByProviderAndProviderUserIdAsync(
            provider, oauthUser.ProviderId, cancellationToken);

        User user;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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

                // Ensure admin status is correct
                if (!user.IsAdmin && oauthUser.Email == adminConfig.AdminEmail)
                {
                    await userRepository.SetAdminStatusAsync(user.Id, true);
                    user = user with { IsAdmin = true };
                }
            }
            else
            {
                // Orphaned OAuth provider record (user was deleted). Clean it up and link to existing user if available.
                logger.LogDebug("OAuth provider record found but user missing. Deleting orphaned record.");
                await oauthProviderRepository.DeleteAsync(oauthProviderRecord.Id);

                // Check if user exists by email and link provider to them
                User? userByEmail = await userRepository.GetByEmailAsync(oauthUser.Email);

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
                        UpdatedAt = now,
                        IsAdmin = oauthUser.Email == adminConfig.AdminEmail
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

                user = new User
                {
                    Id = Guid.CreateVersion7().ToString(),
                    DisplayName = oauthUser.DisplayName,
                    Email = oauthUser.Email,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsAdmin = oauthUser.Email == adminConfig.AdminEmail
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
        return new Result<User>(user);
    }

    public async Task<Result<User>> RegisterAsync(string email, string password, string? displayName = null)
    {
        logger.LogTrace("Registration attempt for email: {email}", email);

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            return new Result<User>(Results.BadRequest("Email is required"));
        }

        // Validate password strength
        string? passwordError = passwordAuthService.ValidatePasswordStrength(password);
        if (passwordError is not null)
        {
            return new Result<User>(Results.BadRequest(passwordError));
        }

        // Check if user already exists
        User? existingUser = await userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            return new Result<User>(Results.BadRequest("An account with this email already exists"));
        }

        // Hash the password
        string passwordHash = passwordAuthService.HashPassword(password);

        // Create new user
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User newUser = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            Email = email,
            DisplayName = displayName ?? email.Split('@')[0],
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now,
            IsAdmin = email == adminConfig.AdminEmail
        };

        await userRepository.CreateAsync(newUser);
        logger.LogTrace("Created new user: {userId}", newUser.Id);

        return new Result<User>(newUser);
    }

    public async Task<Result<User>> LoginWithPasswordAsync(string email, string password)
    {
        logger.LogTrace("Password login attempt for email: {email}", email);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new Result<User>(Results.BadRequest("Email and password are required"));
        }

        // Find user by email
        User? user = await userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            // Don't reveal whether user exists
            return new Result<User>(Results.BadRequest("Invalid email or password"));
        }

        // Check if user has a password set
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return new Result<User>(Results.BadRequest("This account uses OAuth login. Please use the OAuth buttons to log in."));
        }

        // Verify password
        bool passwordValid = passwordAuthService.VerifyPassword(password, user.PasswordHash);
        if (!passwordValid)
        {
            logger.LogDebug("Invalid password for user: {userId}", user.Id);
            return new Result<User>(Results.BadRequest("Invalid email or password"));
        }

        logger.LogTrace("Password verified for user: {userId}", user.Id);
        return new Result<User>(user);
    }

    public async Task<Session> CreateSessionAsync(string userId, long durationSeconds)
    {
        Session session = await sessionRepository.CreateAsync(userId, (int)durationSeconds);
        logger.LogTrace("Session created: {sessionId}", session.Id);
        return session;
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        await sessionRepository.DeleteAsync(sessionId);
    }
}
