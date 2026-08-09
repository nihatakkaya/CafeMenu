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

    public async Task AddAsync(CafeEntity cafe, CancellationToken cancellationToken)
    {
        await _dbContext.Cafes.AddAsync(cafe, cancellationToken);
    }
}
