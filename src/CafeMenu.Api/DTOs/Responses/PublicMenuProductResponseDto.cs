namespace CafeMenu.Api.DTOs.Responses;

public sealed record PublicMenuProductResponseDto(
    long Id,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable,
    int DisplayOrder);
