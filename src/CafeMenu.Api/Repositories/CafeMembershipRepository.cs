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

    public Task<CafeMembershipEntity?> GetActiveMembershipForUserCafeAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CafeMemberships
            .Include(membership => membership.AppUser)
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

    public Task<CafeMembershipEntity?> GetByIdWithUserCafeRoleAsync(
        long membershipId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CafeMemberships
            .Include(membership => membership.AppUser)
            .Include(membership => membership.Cafe)
            .Include(membership => membership.Role)
            .FirstOrDefaultAsync(
                membership => membership.Id == membershipId && !membership.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<CafeMembershipEntity>> GetActiveMembershipsForCafeAsync(
        long cafeId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CafeMemberships
            .AsNoTracking()
            .Include(membership => membership.AppUser)
            .Include(membership => membership.Role)
            .Where(membership =>
                membership.CafeId == cafeId &&
                membership.IsActive &&
                !membership.IsDeleted &&
                !membership.AppUser.IsDeleted)
            .OrderBy(membership => membership.AppUser.FullName)
            .ThenBy(membership => membership.AppUser.Email)
            .ThenBy(membership => membership.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CafeMembershipEntity>> GetActiveMembershipsForUserAsync(
        long appUserId,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CafeMemberships
            .AsNoTracking()
            .Include(membership => membership.Cafe)
            .Include(membership => membership.Role)
            .Where(membership =>
                membership.AppUserId == appUserId &&
                membership.IsActive &&
                !membership.IsDeleted &&
                membership.Cafe.IsActive &&
                !membership.Cafe.IsDeleted &&
                roleCodes.Contains(membership.Role.Code))
            .OrderBy(membership => membership.Cafe.Name)
            .ThenBy(membership => membership.Cafe.Id)
            .ThenBy(membership => membership.Role.Code)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(CafeMembershipEntity membership, CancellationToken cancellationToken)
    {
        await _dbContext.CafeMemberships.AddAsync(membership, cancellationToken);
    }
}
