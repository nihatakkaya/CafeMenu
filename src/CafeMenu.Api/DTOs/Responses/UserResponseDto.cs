namespace CafeMenu.Api.DTOs.Responses;

public sealed record UserResponseDto(
    long Id,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles);
