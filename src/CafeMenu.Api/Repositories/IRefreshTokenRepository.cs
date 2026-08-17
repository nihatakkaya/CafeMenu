using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenEntity?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken);
}
