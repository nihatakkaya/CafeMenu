namespace CafeMenu.Api.Entities;

public sealed class ProductEntity
{
    public long Id { get; set; }

    public long CafeId { get; set; }

    public long CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsVisible { get; set; } = true;

    public bool IsPublished { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public CafeEntity Cafe { get; set; } = null!;

    public CategoryEntity Category { get; set; } = null!;
}
