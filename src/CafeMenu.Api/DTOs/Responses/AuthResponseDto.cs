namespace CafeMenu.Api.DTOs.Responses;

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    UserResponseDto User);
