namespace CafeMenu.Api.DTOs.Responses;

public sealed record MyCafeResponseDto(
    long Id,
    string Name,
    string Slug,
    string? LogoImageUrl,
    bool IsActive,
    bool IsPublished,
    IReadOnlyCollection<string> RoleCodes);
