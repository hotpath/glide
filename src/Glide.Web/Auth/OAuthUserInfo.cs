namespace Glide.Web.Auth;

public record OAuthUserInfo(
    string ProviderId,      // Provider's user ID as string
    string Username,        // Login username
    string Email,          // User email
    string DisplayName     // Full name for display
);