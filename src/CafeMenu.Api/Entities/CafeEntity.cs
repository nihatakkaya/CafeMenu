namespace CafeMenu.Api.Entities;

public sealed class CafeEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? LogoImageUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<CafeMembershipEntity> Memberships { get; } = new List<CafeMembershipEntity>();

    public ICollection<CategoryEntity> Categories { get; } = new List<CategoryEntity>();
}
