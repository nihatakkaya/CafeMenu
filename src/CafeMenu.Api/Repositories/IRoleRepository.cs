using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface IRoleRepository
{
    Task<RoleEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}
