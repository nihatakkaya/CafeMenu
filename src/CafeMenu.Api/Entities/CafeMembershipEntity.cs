namespace CafeMenu.Api.Entities;

public sealed class CafeMembershipEntity
{
    public long Id { get; set; }

    public long AppUserId { get; set; }

    public long CafeId { get; set; }

    public long RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public AppUserEntity AppUser { get; set; } = null!;

    public CafeEntity Cafe { get; set; } = null!;

    public RoleEntity Role { get; set; } = null!;
}
