namespace CafeMenu.Web.AdminAuth;

public sealed record AdminSessionTokens(
    string SessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
