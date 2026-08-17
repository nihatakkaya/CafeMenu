using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public ProductRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductEntity?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductEntity>> GetByCafeIdAsync(
        long cafeId,
        long? categoryId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Products
            .Where(product => product.CafeId == cafeId && !product.IsDeleted);

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(product => product.CategoryId)
            .ThenBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductEntity>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Where(product => ids.Contains(product.Id) && !product.IsDeleted)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(ProductEntity product, CancellationToken cancellationToken)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }
}
