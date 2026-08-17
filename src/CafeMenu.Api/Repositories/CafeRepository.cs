using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class CafeRepository : ICafeRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public CafeRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
    {
        return _dbContext.Cafes.AnyAsync(cafe => cafe.Slug == slug, cancellationToken);
    }

    public Task<CafeEntity?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Cafes.FirstOrDefaultAsync(cafe => cafe.Id == id && !cafe.IsDeleted, cancellationToken);
    }

    public Task<CafeEntity?> GetByIdWithMembershipsAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Cafes
            .Include(cafe => cafe.Memberships.Where(membership => !membership.IsDeleted))
            .ThenInclude(membership => membership.AppUser)
            .Include(cafe => cafe.Memberships.Where(membership => !membership.IsDeleted))
            .ThenInclude(membership => membership.Role)
            .FirstOrDefaultAsync(cafe => cafe.Id == id && !cafe.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CafeEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Cafes
            .Where(cafe => !cafe.IsDeleted)
            .OrderBy(cafe => cafe.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CafeDashboardStatsProjection?> GetDashboardStatsAsync(
        long cafeId,
        CancellationToken cancellationToken)
    {
        var cafe = await _dbContext.Cafes
            .Where(cafe => cafe.Id == cafeId && !cafe.IsDeleted)
            .Select(cafe => new
            {
                cafe.Id,
                cafe.Name,
                cafe.IsActive,
                cafe.IsPublished
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cafe is null)
        {
            return null;
        }

        var categoryStats = await _dbContext.Categories
            .Where(category => category.CafeId == cafeId && !category.IsDeleted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                PublicCount = group.Count(category => category.IsVisible && category.IsPublished)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var productStats = await _dbContext.Products
            .Where(product => product.CafeId == cafeId && !product.IsDeleted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                PublicCount = group.Count(product => product.IsVisible && product.IsPublished),
                AvailableCount = group.Count(product => product.IsAvailable),
                UnavailableCount = group.Count(product => !product.IsAvailable)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new CafeDashboardStatsProjection(
            cafe.Id,
            cafe.Name,
            cafe.IsActive,
            cafe.IsPublished,
            categoryStats?.TotalCount ?? 0,
            categoryStats?.PublicCount ?? 0,
            productStats?.TotalCount ?? 0,
            productStats?.PublicCount ?? 0,
            productStats?.AvailableCount ?? 0,
            productStats?.UnavailableCount ?? 0);
    }

    public async Task<PlatformDashboardStatsProjection> GetPlatformDashboardStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await _dbContext.Cafes
            .Where(cafe => !cafe.IsDeleted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                ActiveCafeCount = group.Count(cafe => cafe.IsActive),
                InactiveCafeCount = group.Count(cafe => !cafe.IsActive),
                PublishedCafeCount = group.Count(cafe => cafe.IsPublished),
                DraftCafeCount = group.Count(cafe => !cafe.IsPublished)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats is null
            ? new PlatformDashboardStatsProjection(0, 0, 0, 0)
            : new PlatformDashboardStatsProjection(
                stats.ActiveCafeCount,
                stats.InactiveCafeCount,
                stats.PublishedCafeCount,
                stats.DraftCafeCount);
    }

    public async Task AddAsync(CafeEntity cafe, CancellationToken cancellationToken)
    {
        await _dbContext.Cafes.AddAsync(cafe, cancellationToken);
    }
}
