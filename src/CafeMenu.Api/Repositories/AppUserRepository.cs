using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class AppUserRepository : IAppUserRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public AppUserRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> EmailExistsIncludingDeletedAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .IgnoreQueryFilters()
            .AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<AppUserEntity?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<AppUserEntity?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AppUserEntity>> SearchForPlatformOnboardingAsync(
        string query,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim().ToLowerInvariant();

        return await _dbContext.AppUsers
            .AsNoTracking()
            .Where(user => user.IsActive)
            .Where(user =>
                user.Email.ToLower().Contains(normalizedQuery) ||
                user.FullName.ToLower().Contains(normalizedQuery))
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(AppUserEntity user, CancellationToken cancellationToken)
    {
        await _dbContext.AppUsers.AddAsync(user, cancellationToken);
    }
}
