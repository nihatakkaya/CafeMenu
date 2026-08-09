namespace CafeMenu.Api.DTOs.Responses;

public sealed record CategoryResponseDto(
    long Id,
    long CafeId,
    string Name,
    string? Description,
    string? ImageUrl,
    int DisplayOrder,
    bool IsVisible,
    bool IsPublished,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
