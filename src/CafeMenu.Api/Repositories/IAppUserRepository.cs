using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface IAppUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsIncludingDeletedAsync(string email, CancellationToken cancellationToken);

    Task<AppUserEntity?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken);

    Task<AppUserEntity?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(AppUserEntity user, CancellationToken cancellationToken);
}
