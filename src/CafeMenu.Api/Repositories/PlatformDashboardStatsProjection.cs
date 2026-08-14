namespace CafeMenu.Api.Repositories;

public sealed record PlatformDashboardStatsProjection(
    int ActiveCafeCount,
    int InactiveCafeCount,
    int PublishedCafeCount,
    int DraftCafeCount);
