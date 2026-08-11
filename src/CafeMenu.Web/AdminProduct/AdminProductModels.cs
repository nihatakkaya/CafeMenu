using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminProduct;

public sealed class AdminProductResponse
{
    public long Id { get; init; }

    public long CafeId { get; init; }

    public long CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public bool IsAvailable { get; init; }

    public bool IsVisible { get; init; }

    public bool IsPublished { get; init; }

    public int DisplayOrder { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class AdminProductFormModel
{
    private string? _description;
    private string? _imageUrl;

    public long ProductId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Kategori seçilmelidir.")]
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [StringLength(180, MinimumLength = 2, ErrorMessage = "Ürün adı 2 ile 180 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description
    {
        get => _description;
        set => _description = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Fiyat 0 veya daha büyük olmalıdır.")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Görsel referansı en fazla 500 karakter olabilir.")]
    [Url(ErrorMessage = "Görsel referansı geçerli bir URL olmalıdır.")]
    public string? ImageUrl
    {
        get => _imageUrl;
        set => _imageUrl = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [Range(0, int.MaxValue, ErrorMessage = "Sıralama değeri 0 veya daha büyük olmalıdır.")]
    public int DisplayOrder { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsVisible { get; set; } = true;

    public static AdminProductFormModel FromProduct(AdminProductResponse product)
    {
        return new AdminProductFormModel
        {
            ProductId = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            DisplayOrder = product.DisplayOrder,
            IsAvailable = product.IsAvailable,
            IsVisible = product.IsVisible
        };
    }
}

public sealed class AdminProductActionFormModel
{
    public string? Action { get; set; }
}

public sealed record AdminCreateProductRequest(
    long CafeId,
    long CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable,
    bool IsVisible,
    int DisplayOrder);

public sealed record AdminUpdateProductRequest(
    long CafeId,
    long CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int DisplayOrder);

public sealed record AdminChangeProductVisibilityRequest(long CafeId, bool IsVisible);

public sealed record AdminChangeProductAvailabilityRequest(long CafeId, bool IsAvailable);

public sealed record AdminChangeProductPublicationRequest(long CafeId, bool IsPublished);

public sealed record AdminReorderProductsRequest(long CafeId, long CategoryId, IReadOnlyCollection<AdminProductOrderRequest> Products);

public sealed record AdminProductOrderRequest(long ProductId, int DisplayOrder);
