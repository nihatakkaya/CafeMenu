using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface ICategoryRepository
{
    Task<CategoryEntity?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CategoryEntity>> GetByCafeIdAsync(long cafeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CategoryEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);

    Task AddAsync(CategoryEntity category, CancellationToken cancellationToken);
}
