namespace CafeMenu.Api.DTOs.Responses;

public sealed record CafeBrandingResponseDto(
    long CafeId,
    string CafeName,
    string? LogoImageUrl,
    string? CoverImageUrl,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string? WelcomeTitle,
    string? WelcomeDescription,
    string FontPreset,
    string ThemePreset,
    bool IsPublished,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
