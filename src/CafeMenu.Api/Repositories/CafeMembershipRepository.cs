using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class CafeMembershipRepository : ICafeMembershipRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public CafeMembershipRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ActiveMembershipExistsAsync(long appUserId, long cafeId, CancellationToken cancellationToken)
    {
        return _dbContext.CafeMemberships.AnyAsync(
            membership =>
                membership.AppUserId == appUserId &&
                membership.CafeId == cafeId &&
                membership.IsActive &&
                !membership.IsDeleted,
            cancellationToken);
    }

    public Task<CafeMembershipEntity?> GetActiveMembershipAsync(long appUserId, long cafeId, CancellationToken cancellationToken)
    {
        return _dbContext.CafeMemberships
            .Include(membership => membership.Cafe)
            .Include(membership => membership.Role)
            .FirstOrDefaultAsync(
                membership =>
                    membership.AppUserId == appUserId &&
                    membership.CafeId == cafeId &&
                    membership.IsActive &&
                    !membership.IsDeleted,
                cancellationToken);
    }

    public async Task AddAsync(CafeMembershipEntity membership, CancellationToken cancellationToken)
    {
        await _dbContext.CafeMemberships.AddAsync(membership, cancellationToken);
    }
}
