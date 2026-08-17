using System.ComponentModel.DataAnnotations;
using CafeMenu.Api.Common;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class UpdateCafeBrandingRequest
{
    [StringLength(500)]
    [NoUnsafeMarkup]
    public string? LogoImageUrl { get; init; }

    [StringLength(500)]
    [NoUnsafeMarkup]
    public string? CoverImageUrl { get; init; }

    [Required]
    [RegularExpression(CafeThemeConstants.HexColorPattern)]
    public string PrimaryColor { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CafeThemeConstants.HexColorPattern)]
    public string SecondaryColor { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CafeThemeConstants.HexColorPattern)]
    public string AccentColor { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CafeThemeConstants.HexColorPattern)]
    public string BackgroundColor { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CafeThemeConstants.HexColorPattern)]
    public string TextColor { get; init; } = string.Empty;

    [StringLength(120)]
    [NoUnsafeMarkup]
    public string? WelcomeTitle { get; init; }

    [StringLength(500)]
    [NoUnsafeMarkup]
    public string? WelcomeDescription { get; init; }

    [Required]
    [RegularExpression(CafeThemeConstants.FontPresetPattern)]
    public string FontPreset { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CafeThemeConstants.ThemePresetPattern)]
    public string ThemePreset { get; init; } = string.Empty;

    public bool IsPublished { get; init; }
}
