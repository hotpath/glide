namespace Glide.Data.UserOAuthProviders;

public record UserOAuthProvider
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Provider { get; init; }
    public required string ProviderUserId { get; init; }
    public string? ProviderEmail { get; init; }
    public long CreatedAt { get; init; }
    public long UpdatedAt { get; init; }
}
