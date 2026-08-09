using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public CategoryRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CategoryEntity?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id && !category.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CategoryEntity>> GetByCafeIdAsync(long cafeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .Where(category => category.CafeId == cafeId && !category.IsDeleted)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CategoryEntity>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .Where(category => ids.Contains(category.Id) && !category.IsDeleted)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(CategoryEntity category, CancellationToken cancellationToken)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);
    }
}
