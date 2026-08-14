namespace CafeMenu.Web.AdminCafe;

public enum AdminCafeListStatus
{
    Success,
    Failure
}

public sealed record AdminCafeListResult(
    AdminCafeListStatus Status,
    IReadOnlyCollection<AdminCafeResponse> Cafes)
{
    public static AdminCafeListResult Success(IReadOnlyCollection<AdminCafeResponse> cafes)
    {
        return new AdminCafeListResult(AdminCafeListStatus.Success, cafes);
    }

    public static AdminCafeListResult Failure()
    {
        return new AdminCafeListResult(AdminCafeListStatus.Failure, []);
    }
}

public sealed record AdminCafeDashboardStatsResult(
    AdminCafeListStatus Status,
    AdminCafeDashboardStatsResponse? Stats)
{
    public static AdminCafeDashboardStatsResult Success(AdminCafeDashboardStatsResponse stats)
    {
        return new AdminCafeDashboardStatsResult(AdminCafeListStatus.Success, stats);
    }

    public static AdminCafeDashboardStatsResult Failure()
    {
        return new AdminCafeDashboardStatsResult(AdminCafeListStatus.Failure, null);
    }
}
