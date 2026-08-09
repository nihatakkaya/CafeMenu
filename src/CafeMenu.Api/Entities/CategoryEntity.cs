namespace CafeMenu.Api.Entities;

public sealed class CategoryEntity
{
    public long Id { get; set; }

    public long CafeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public CafeEntity Cafe { get; set; } = null!;
}
