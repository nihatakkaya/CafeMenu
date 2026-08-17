using System.Text.RegularExpressions;

namespace CafeMenu.Web.PublicMenu;

public static partial class PublicMenuThemeStyleBuilder
{
    private const string DefaultPrimaryColor = "#111827";
    private const string DefaultSecondaryColor = "#F9FAFB";
    private const string DefaultAccentColor = "#D97706";
    private const string DefaultBackgroundColor = "#FFFFFF";
    private const string DefaultTextColor = "#111827";

    public static string BuildStyle(PublicMenuThemeResponse theme)
    {
        return string.Join(
            ' ',
            $"--cafe-primary-color: {SafeHex(theme.PrimaryColor, DefaultPrimaryColor)};",
            $"--cafe-secondary-color: {SafeHex(theme.SecondaryColor, DefaultSecondaryColor)};",
            $"--cafe-accent-color: {SafeHex(theme.AccentColor, DefaultAccentColor)};",
            $"--cafe-background-color: {SafeHex(theme.BackgroundColor, DefaultBackgroundColor)};",
            $"--cafe-text-color: {SafeHex(theme.TextColor, DefaultTextColor)};");
    }

    public static string GetThemePresetClass(string? themePreset)
    {
        return themePreset switch
        {
            "MODERN" => "theme-modern",
            "COMPACT" => "theme-compact",
            _ => "theme-classic"
        };
    }

    public static string GetFontPresetClass(string? fontPreset)
    {
        return fontPreset switch
        {
            "SANS" => "font-sans",
            "SERIF" => "font-serif",
            _ => "font-system"
        };
    }

    private static string SafeHex(string? value, string fallback)
    {
        return value is not null && HexColorRegex().IsMatch(value) ? value : fallback;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}
