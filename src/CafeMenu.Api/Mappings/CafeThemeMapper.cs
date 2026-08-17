using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class CafeThemeMapper
{
    public CafeBrandingResponseDto ToResponse(CafeEntity cafe, CafeThemeEntity? theme)
    {
        var resolvedTheme = theme ?? CreateDefaultTheme(cafe.Id);

        return new CafeBrandingResponseDto(
            cafe.Id,
            cafe.Name,
            cafe.LogoImageUrl,
            cafe.CoverImageUrl,
            resolvedTheme.PrimaryColor,
            resolvedTheme.SecondaryColor,
            resolvedTheme.AccentColor,
            resolvedTheme.BackgroundColor,
            resolvedTheme.TextColor,
            resolvedTheme.WelcomeTitle,
            resolvedTheme.WelcomeDescription,
            resolvedTheme.FontPreset,
            resolvedTheme.ThemePreset,
            resolvedTheme.IsPublished,
            resolvedTheme.CreatedAt,
            resolvedTheme.UpdatedAt);
    }

    public static CafeThemeEntity CreateDefaultTheme(long cafeId)
    {
        var utcNow = DateTimeOffset.UtcNow;

        return new CafeThemeEntity
        {
            CafeId = cafeId,
            PrimaryColor = CafeThemeConstants.DefaultPrimaryColor,
            SecondaryColor = CafeThemeConstants.DefaultSecondaryColor,
            AccentColor = CafeThemeConstants.DefaultAccentColor,
            BackgroundColor = CafeThemeConstants.DefaultBackgroundColor,
            TextColor = CafeThemeConstants.DefaultTextColor,
            FontPreset = CafeThemeConstants.SystemFontPreset,
            ThemePreset = CafeThemeConstants.ClassicThemePreset,
            IsPublished = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }
}
