namespace CafeMenu.Api.Entities;

public sealed class RefreshTokenEntity
{
    public long Id { get; set; }

    public long AppUserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public AppUserEntity AppUser { get; set; } = null!;

    public bool IsActive(DateTimeOffset utcNow)
    {
        return RevokedAt is null && ExpiresAt > utcNow;
    }
}
