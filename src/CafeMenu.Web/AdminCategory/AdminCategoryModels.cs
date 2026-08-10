using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminCategory;

public sealed class AdminCategoryResponse
{
    public long Id { get; init; }

    public long CafeId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsVisible { get; init; }

    public bool IsPublished { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class AdminCategoryFormModel
{
    private string? _imageUrl;

    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(160, MinimumLength = 2, ErrorMessage = "Kategori adı 2 ile 160 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Görsel referansı en fazla 500 karakter olabilir.")]
    [Url(ErrorMessage = "Görsel referansı geçerli bir URL olmalıdır.")]
    public string? ImageUrl
    {
        get => _imageUrl;
        set => _imageUrl = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [Range(0, int.MaxValue, ErrorMessage = "Sıralama değeri 0 veya daha büyük olmalıdır.")]
    public int DisplayOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public static AdminCategoryFormModel FromCategory(AdminCategoryResponse category)
    {
        return new AdminCategoryFormModel
        {
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            DisplayOrder = category.DisplayOrder,
            IsVisible = category.IsVisible,
            CategoryId = category.Id
        };
    }
}

public sealed class AdminCategoryActionFormModel
{
    public string? Action { get; set; }
}

public sealed record AdminCreateCategoryRequest(
    long CafeId,
    string Name,
    string? Description,
    string? ImageUrl,
    int DisplayOrder,
    bool IsVisible);

public sealed record AdminUpdateCategoryRequest(
    long CafeId,
    string Name,
    string? Description,
    string? ImageUrl,
    int DisplayOrder);

public sealed record AdminChangeCategoryVisibilityRequest(long CafeId, bool IsVisible);

public sealed record AdminReorderCategoriesRequest(long CafeId, IReadOnlyCollection<AdminCategoryOrderRequest> Categories);

public sealed record AdminCategoryOrderRequest(long CategoryId, int DisplayOrder);
