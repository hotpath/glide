namespace Glide.Data.SiteSettings;

public record SiteSetting
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public long CreatedAt { get; init; }
    public long UpdatedAt { get; init; }
}