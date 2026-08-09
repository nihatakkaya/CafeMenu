using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class PublicMenuRepository : IPublicMenuRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public PublicMenuRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CafeEntity?> GetPublishedMenuBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return _dbContext.Cafes
            .AsNoTracking()
            .Include(cafe => cafe.Theme)
            .Include(cafe => cafe.Categories
                .Where(category => category.IsVisible && category.IsPublished && !category.IsDeleted))
            .ThenInclude(category => category.Products
                .Where(product => product.IsVisible && product.IsPublished && !product.IsDeleted))
            .FirstOrDefaultAsync(
                cafe => cafe.Slug == slug && cafe.IsActive && cafe.IsPublished && !cafe.IsDeleted,
                cancellationToken);
    }
}
