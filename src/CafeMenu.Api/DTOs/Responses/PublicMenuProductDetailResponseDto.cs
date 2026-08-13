namespace CafeMenu.Api.DTOs.Responses;

public sealed record PublicMenuProductDetailResponseDto(
    string CafeName,
    string Slug,
    string? LogoImageUrl,
    string? CoverImageUrl,
    PublicMenuThemeResponseDto Theme,
    long CategoryId,
    string CategoryName,
    long ProductId,
    string ProductName,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable);
