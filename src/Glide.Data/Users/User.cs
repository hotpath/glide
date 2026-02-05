namespace Glide.Data.Users;

public record User
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public required string Email { get; init; }
    public string? PasswordHash { get; init; }
    public long CreatedAt { get; init; }
    public long UpdatedAt { get; init; }
    public bool IsAdmin { get; init; }
}