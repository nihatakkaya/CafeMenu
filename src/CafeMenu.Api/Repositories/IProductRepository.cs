using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface IProductRepository
{
    Task<ProductEntity?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductEntity>> GetByCafeIdAsync(long cafeId, long? categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);

    Task AddAsync(ProductEntity product, CancellationToken cancellationToken);
}
