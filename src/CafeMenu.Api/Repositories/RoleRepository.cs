using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public RoleRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RoleEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _dbContext.Roles.FirstOrDefaultAsync(role => role.Code == code, cancellationToken);
    }
}
