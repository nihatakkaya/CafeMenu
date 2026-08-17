namespace CafeMenu.Web.AdminAuth;

public interface IAdminAuthApiClient
{
    Task<AdminAuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<AdminAuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
