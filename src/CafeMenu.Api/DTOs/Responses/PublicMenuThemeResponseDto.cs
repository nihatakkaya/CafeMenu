namespace CafeMenu.Api.DTOs.Responses;

public sealed record PublicMenuThemeResponseDto(
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string? WelcomeTitle,
    string? WelcomeDescription,
    string FontPreset,
    string ThemePreset);
