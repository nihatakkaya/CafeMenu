using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface IPublicMenuRepository
{
    Task<CafeEntity?> GetPublishedMenuBySlugAsync(string slug, CancellationToken cancellationToken);
}
