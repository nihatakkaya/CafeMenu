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

    public Task<ProductEntity?> GetPublishedProductDetailAsync(
        string slug,
        long productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Cafe)
                .ThenInclude(cafe => cafe.Theme)
            .Include(product => product.Category)
            .FirstOrDefaultAsync(
                product =>
                    product.Id == productId &&
                    product.Cafe.Slug == slug &&
                    product.Cafe.IsActive &&
                    product.Cafe.IsPublished &&
                    !product.Cafe.IsDeleted &&
                    product.Category.CafeId == product.CafeId &&
                    product.Category.IsVisible &&
                    product.Category.IsPublished &&
                    !product.Category.IsDeleted &&
                    product.IsVisible &&
                    product.IsPublished &&
                    !product.IsDeleted,
                cancellationToken);
    }
}
