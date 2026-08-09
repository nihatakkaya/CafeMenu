using System.Security.Claims;
using System.Security.Cryptography;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminAuthService : IAdminAuthService
{
    private readonly IAdminAuthApiClient _authApiClient;
    private readonly IAdminSessionTokenStore _tokenStore;

    public AdminAuthService(
        IAdminAuthApiClient authApiClient,
        IAdminSessionTokenStore tokenStore)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
    }

    public async Task<AdminLoginResult> LoginAsync(
        AdminLoginCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return AdminLoginResult.Failure();
        }

        var authResponse = await _authApiClient.LoginAsync(
            command.Email.Trim(),
            command.Password,
            cancellationToken);

        if (authResponse is null)
        {
            return AdminLoginResult.Failure();
        }

        var sessionId = CreateSessionId();
        await _tokenStore.StoreAsync(
            new AdminSessionTokens(
                sessionId,
                authResponse.AccessToken,
                authResponse.AccessTokenExpiresAt,
                authResponse.RefreshToken,
                authResponse.RefreshTokenExpiresAt),
            cancellationToken);

        var identity = new ClaimsIdentity(
            CreateClaims(authResponse.User, sessionId),
            AdminAuthenticationConstants.CookieScheme);

        return AdminLoginResult.Success(new ClaimsPrincipal(identity));
    }

    public async Task LogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var sessionId = principal.FindFirstValue(AdminAuthenticationConstants.SessionIdClaim);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var tokens = await _tokenStore.GetAsync(sessionId, cancellationToken);
        if (tokens is not null)
        {
            await _authApiClient.LogoutAsync(tokens.RefreshToken, cancellationToken);
        }

        await _tokenStore.RemoveAsync(sessionId, cancellationToken);
    }

    private static IEnumerable<Claim> CreateClaims(AdminUserResponse user, string sessionId)
    {
        yield return new Claim(AdminAuthenticationConstants.AppUserIdClaim, user.Id.ToString());
        yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
        yield return new Claim(ClaimTypes.Email, user.Email);
        yield return new Claim(ClaimTypes.Name, user.FullName);
        yield return new Claim(AdminAuthenticationConstants.SessionIdClaim, sessionId);

        foreach (var role in user.Roles)
        {
            yield return new Claim(ClaimTypes.Role, role);
        }
    }

    private static string CreateSessionId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
