namespace CafeMenu.Web.AdminAuth;

public sealed record AdminAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    AdminUserResponse User);
