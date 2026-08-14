namespace CafeMenu.Api.DTOs.Responses;

public sealed record PlatformDashboardStatsResponseDto(
    int ActiveCafeCount,
    int InactiveCafeCount,
    int PublishedCafeCount,
    int DraftCafeCount);
