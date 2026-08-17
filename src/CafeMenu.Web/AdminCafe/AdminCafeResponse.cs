namespace CafeMenu.Web.AdminCafe;

public sealed class AdminCafeResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? LogoImageUrl { get; init; }

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public IReadOnlyCollection<string> RoleCodes { get; init; } = [];
}

public sealed class AdminCafeDashboardStatsResponse
{
    public long CafeId { get; init; }

    public string CafeName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public int TotalCategoryCount { get; init; }

    public int PublicCategoryCount { get; init; }

    public int TotalProductCount { get; init; }

    public int PublicProductCount { get; init; }

    public int AvailableProductCount { get; init; }

    public int UnavailableProductCount { get; init; }
}
