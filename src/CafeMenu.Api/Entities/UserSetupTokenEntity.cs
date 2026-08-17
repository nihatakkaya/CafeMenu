namespace CafeMenu.Api.Entities;

public sealed class UserSetupTokenEntity
{
    public long Id { get; set; }

    public long AppUserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public AppUserEntity AppUser { get; set; } = null!;
}
