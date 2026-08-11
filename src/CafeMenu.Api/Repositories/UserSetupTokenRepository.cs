using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Repositories;

public sealed class UserSetupTokenRepository : IUserSetupTokenRepository
{
    private readonly CafeMenuDbContext _dbContext;

    public UserSetupTokenRepository(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> TokenHashExistsAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.UserSetupTokens.AnyAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public Task<UserSetupTokenEntity?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.UserSetupTokens
            .Include(token => token.AppUser)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserSetupTokenEntity>> GetUnconsumedByUserIdAsync(
        long appUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UserSetupTokens
            .Where(token => token.AppUserId == appUserId && token.ConsumedAt == null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(UserSetupTokenEntity setupToken, CancellationToken cancellationToken)
    {
        await _dbContext.UserSetupTokens.AddAsync(setupToken, cancellationToken);
    }
}
