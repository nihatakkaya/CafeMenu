namespace CafeMenu.Api.Common;

public static class CafeThemeConstants
{
    public const string ClassicThemePreset = "CLASSIC";
    public const string ModernThemePreset = "MODERN";
    public const string CompactThemePreset = "COMPACT";
    public const string ThemePresetPattern = "^(CLASSIC|MODERN|COMPACT)$";

    public const string SystemFontPreset = "SYSTEM";
    public const string SansFontPreset = "SANS";
    public const string SerifFontPreset = "SERIF";
    public const string FontPresetPattern = "^(SYSTEM|SANS|SERIF)$";

    public const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    public const string DefaultPrimaryColor = "#111827";
    public const string DefaultSecondaryColor = "#F9FAFB";
    public const string DefaultAccentColor = "#D97706";
    public const string DefaultBackgroundColor = "#FFFFFF";
    public const string DefaultTextColor = "#111827";
}
