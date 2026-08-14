namespace CafeMenu.Web.AdminPlatform;

public interface IAdminPlatformApiClient
{
    Task<AdminPlatformCafeListResult> GetCafesAsync(CancellationToken cancellationToken);

    Task<AdminPlatformDashboardStatsResult> GetPlatformDashboardStatsAsync(CancellationToken cancellationToken);

    Task<AdminPlatformCafeMutationResult> CreateCafeAsync(
        AdminPlatformCreateCafeRequest request,
        CancellationToken cancellationToken);

    Task<AdminPlatformCafeMutationResult> ActivateCafeAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminPlatformCafeMutationResult> DeactivateCafeAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminPlatformMemberListResult> GetCafeMembersAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminPlatformUserSetupResult> CreateUserSetupAsync(
        AdminPlatformCreateUserSetupRequest request,
        CancellationToken cancellationToken);

    Task<AdminPlatformUserSetupResult> ReissueUserSetupAsync(long userId, CancellationToken cancellationToken);

    Task<AdminPlatformUserSearchResult> SearchUsersAsync(
        AdminPlatformUserSearchRequest request,
        CancellationToken cancellationToken);

    Task<AdminPlatformMembershipMutationResult> AssignCafeOwnerAsync(
        AdminPlatformAssignCafeMemberRequest request,
        CancellationToken cancellationToken);

    Task<AdminPlatformMembershipMutationResult> AssignCafeManagerAsync(
        AdminPlatformAssignCafeMemberRequest request,
        CancellationToken cancellationToken);

    Task<AdminPlatformMembershipMutationResult> DeactivateCafeMembershipAsync(
        long membershipId,
        CancellationToken cancellationToken);
}
