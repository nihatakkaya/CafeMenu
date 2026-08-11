namespace CafeMenu.Api.DTOs.Responses;

public sealed record PlatformUserResponseDto(
    long Id,
    string Email,
    string FullName,
    bool IsActive);
