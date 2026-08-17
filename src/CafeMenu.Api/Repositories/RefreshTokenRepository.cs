using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public RefreshTokenRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshTokenEntity?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.AppUser)
            .ThenInclude(user => user.Roles)
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }
}
