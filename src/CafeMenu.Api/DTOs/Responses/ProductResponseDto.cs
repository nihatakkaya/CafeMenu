namespace CafeMenu.Api.DTOs.Responses;

public sealed record ProductResponseDto(
    long Id,
    long CafeId,
    long CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable,
    bool IsVisible,
    bool IsPublished,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
