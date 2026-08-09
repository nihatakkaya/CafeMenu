namespace CafeMenu.Api.DTOs.Responses;

public sealed record PublicMenuResponseDto(
    string CafeName,
    string Slug,
    string? LogoImageUrl,
    string? CoverImageUrl,
    PublicMenuThemeResponseDto Theme,
    IReadOnlyCollection<PublicMenuCategoryResponseDto> Categories);
