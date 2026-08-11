using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Repositories;

public interface IUserSetupTokenRepository
{
    Task<bool> TokenHashExistsAsync(string tokenHash, CancellationToken cancellationToken);

    Task<UserSetupTokenEntity?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserSetupTokenEntity>> GetUnconsumedByUserIdAsync(long appUserId, CancellationToken cancellationToken);

    Task AddAsync(UserSetupTokenEntity setupToken, CancellationToken cancellationToken);
}
