using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface ICafeMembershipRepository
{
    Task<bool> ActiveMembershipExistsAsync(long appUserId, long cafeId, CancellationToken cancellationToken);

    Task<CafeMembershipEntity?> GetActiveMembershipAsync(long appUserId, long cafeId, CancellationToken cancellationToken);

    Task<CafeMembershipEntity?> GetActiveMembershipForUserCafeAsync(long appUserId, long cafeId, CancellationToken cancellationToken);

    Task<CafeMembershipEntity?> GetByIdWithUserCafeRoleAsync(long membershipId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CafeMembershipEntity>> GetActiveMembershipsForCafeAsync(long cafeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CafeMembershipEntity>> GetActiveMembershipsForUserAsync(
        long appUserId,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken);

    Task AddAsync(CafeMembershipEntity membership, CancellationToken cancellationToken);
}
