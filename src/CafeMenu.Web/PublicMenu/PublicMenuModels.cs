namespace CafeMenu.Web.PublicMenu;

public sealed class PublicMenuResponse
{
    public string CafeName { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? LogoImageUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public PublicMenuThemeResponse Theme { get; init; } = new();

    public IReadOnlyCollection<PublicMenuCategoryResponse> Categories { get; init; } = [];
}

public sealed class PublicMenuThemeResponse
{
    public string PrimaryColor { get; init; } = string.Empty;

    public string SecondaryColor { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public string BackgroundColor { get; init; } = string.Empty;

    public string TextColor { get; init; } = string.Empty;

    public string? WelcomeTitle { get; init; }

    public string? WelcomeDescription { get; init; }

    public string FontPreset { get; init; } = string.Empty;

    public string ThemePreset { get; init; } = string.Empty;
}

public sealed class PublicMenuCategoryResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public int DisplayOrder { get; init; }

    public IReadOnlyCollection<PublicMenuProductResponse> Products { get; init; } = [];
}

public sealed class PublicMenuProductResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public bool IsAvailable { get; init; }

    public int DisplayOrder { get; init; }
}

public sealed class PublicProductDetailResponse
{
    public string CafeName { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? LogoImageUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public PublicMenuThemeResponse Theme { get; init; } = new();

    public long CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public long ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public bool IsAvailable { get; init; }
}
