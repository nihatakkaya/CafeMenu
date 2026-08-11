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

    public async Task AddAsync(AppUserEntity user, CancellationToken cancellationToken)
    {
        await _dbContext.AppUsers.AddAsync(user, cancellationToken);
    }
}
