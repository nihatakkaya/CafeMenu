namespace CafeMenu.Web.AdminCafe;

public interface IAdminCafeApiClient
{
    Task<AdminCafeListResult> GetMyCafesAsync(CancellationToken cancellationToken);

    Task<AdminCafeDashboardStatsResult> GetCafeDashboardStatsAsync(
        long cafeId,
        CancellationToken cancellationToken);
}
