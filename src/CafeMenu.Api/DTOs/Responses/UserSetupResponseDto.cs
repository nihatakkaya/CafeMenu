namespace CafeMenu.Api.DTOs.Responses;

public sealed record UserSetupResponseDto(
    long UserId,
    string Email,
    string FullName,
    bool IsActive,
    string SetupToken,
    DateTimeOffset SetupTokenExpiresAt);
