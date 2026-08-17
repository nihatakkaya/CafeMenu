using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class CafeThemeRepository : ICafeThemeRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public CafeThemeRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CafeThemeEntity?> GetByCafeIdAsync(long cafeId, CancellationToken cancellationToken)
    {
        return _dbContext.CafeThemes.FirstOrDefaultAsync(theme => theme.CafeId == cafeId, cancellationToken);
    }

    public async Task AddAsync(CafeThemeEntity theme, CancellationToken cancellationToken)
    {
        await _dbContext.CafeThemes.AddAsync(theme, cancellationToken);
    }
}
