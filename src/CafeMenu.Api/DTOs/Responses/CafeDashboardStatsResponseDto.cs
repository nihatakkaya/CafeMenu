namespace CafeMenu.Api.DTOs.Responses;

public sealed record CafeDashboardStatsResponseDto(
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
