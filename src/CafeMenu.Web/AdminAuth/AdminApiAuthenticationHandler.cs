using System.Net.Http.Headers;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminApiAuthenticationHandler : DelegatingHandler
{
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(1);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAdminSessionTokenStore _tokenStore;
    private readonly IAdminAuthApiClient _authApiClient;
    private readonly TimeProvider _timeProvider;

    public AdminApiAuthenticationHandler(
        IHttpContextAccessor httpContextAccessor,
        IAdminSessionTokenStore tokenStore,
        IAdminAuthApiClient authApiClient,
        TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenStore = tokenStore;
        _authApiClient = authApiClient;
        _timeProvider = timeProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sessionId = _httpContextAccessor.HttpContext?.User.FindFirst(AdminAuthenticationConstants.SessionIdClaim)?.Value;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var tokens = await GetUsableTokensAsync(sessionId, cancellationToken);
        if (tokens is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<AdminSessionTokens?> GetUsableTokensAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var tokens = await _tokenStore.GetAsync(sessionId, cancellationToken);
        if (tokens is null)
        {
            return null;
        }

        var refreshIfExpiresBefore = _timeProvider.GetUtcNow().Add(RefreshThreshold);
        if (tokens.AccessTokenExpiresAt > refreshIfExpiresBefore)
        {
            return tokens;
        }

        return await _tokenStore.RefreshAsync(
            sessionId,
            refreshIfExpiresBefore,
            RefreshTokensAsync,
            cancellationToken);
    }

    private async Task<AdminSessionTokens?> RefreshTokensAsync(
        AdminSessionTokens currentTokens,
        CancellationToken cancellationToken)
    {
        var authResponse = await _authApiClient.RefreshTokenAsync(
            currentTokens.RefreshToken,
            cancellationToken);

        return authResponse is null
            ? null
            : new AdminSessionTokens(
                currentTokens.SessionId,
                authResponse.AccessToken,
                authResponse.AccessTokenExpiresAt,
                authResponse.RefreshToken,
                authResponse.RefreshTokenExpiresAt);
    }
}
