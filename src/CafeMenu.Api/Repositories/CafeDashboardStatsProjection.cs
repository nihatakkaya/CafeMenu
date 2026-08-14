namespace CafeMenu.Api.Repositories;

public sealed record CafeDashboardStatsProjection(
    long CafeId,
    string CafeName,
    bool IsActive,
    bool IsPublished,
    int TotalCategoryCount,
    int PublicCategoryCount,
    int TotalProductCount,
    int PublicProductCount,
    int AvailableProductCount,
    int UnavailableProductCount);
