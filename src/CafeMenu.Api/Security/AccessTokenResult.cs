namespace CafeMenu.Api.Security;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
