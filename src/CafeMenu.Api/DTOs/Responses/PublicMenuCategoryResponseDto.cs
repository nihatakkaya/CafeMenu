namespace CafeMenu.Api.DTOs.Responses;

public sealed record PublicMenuCategoryResponseDto(
    long Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int DisplayOrder,
    IReadOnlyCollection<PublicMenuProductResponseDto> Products);
