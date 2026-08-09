using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface ICafeRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<CafeEntity?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<CafeEntity?> GetByIdWithMembershipsAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CafeEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(CafeEntity cafe, CancellationToken cancellationToken);
}
