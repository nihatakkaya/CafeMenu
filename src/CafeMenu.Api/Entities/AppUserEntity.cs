namespace CafeMenu.Api.Entities;

public sealed class AppUserEntity
{
    public long Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<RoleEntity> Roles { get; } = new List<RoleEntity>();

    public ICollection<RefreshTokenEntity> RefreshTokens { get; } = new List<RefreshTokenEntity>();

    public ICollection<CafeMembershipEntity> CafeMemberships { get; } = new List<CafeMembershipEntity>();
}
