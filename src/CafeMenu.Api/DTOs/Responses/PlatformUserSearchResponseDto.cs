namespace CafeMenu.Api.DTOs.Responses;

public sealed record PlatformUserSearchResponseDto(
    long AppUserId,
    string Email,
    string FullName,
    bool IsActive);
