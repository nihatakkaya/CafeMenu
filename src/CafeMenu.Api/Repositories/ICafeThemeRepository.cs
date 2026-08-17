using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface ICafeThemeRepository
{
    Task<CafeThemeEntity?> GetByCafeIdAsync(long cafeId, CancellationToken cancellationToken);

    Task AddAsync(CafeThemeEntity theme, CancellationToken cancellationToken);
}
