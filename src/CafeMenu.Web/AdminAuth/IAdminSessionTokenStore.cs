namespace CafeMenu.Web.AdminAuth;

public interface IAdminSessionTokenStore
{
    Task StoreAsync(AdminSessionTokens tokens, CancellationToken cancellationToken);

    Task<AdminSessionTokens?> GetAsync(string sessionId, CancellationToken cancellationToken);

    Task RemoveAsync(string sessionId, CancellationToken cancellationToken);

    Task<AdminSessionTokens?> RefreshAsync(
        string sessionId,
        DateTimeOffset refreshIfExpiresBefore,
        Func<AdminSessionTokens, CancellationToken, Task<AdminSessionTokens?>> refreshOperation,
        CancellationToken cancellationToken);
}
