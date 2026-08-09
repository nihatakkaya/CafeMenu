using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface ICafeMembershipRepository
{
    Task<bool> ActiveMembershipExistsAsync(long appUserId, long cafeId, CancellationToken cancellationToken);

    Task<CafeMembershipEntity?> GetActiveMembershipAsync(long appUserId, long cafeId, CancellationToken cancellationToken);

    Task AddAsync(CafeMembershipEntity membership, CancellationToken cancellationToken);
}
