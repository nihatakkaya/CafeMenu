using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminBranding;

public static class AdminBrandingConstants
{
    public const string ClassicThemePreset = "CLASSIC";
    public const string ModernThemePreset = "MODERN";
    public const string CompactThemePreset = "COMPACT";

    public const string SystemFontPreset = "SYSTEM";
    public const string SansFontPreset = "SANS";
    public const string SerifFontPreset = "SERIF";

    public const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";
    public const string ThemePresetPattern = "^(CLASSIC|MODERN|COMPACT)$";
    public const string FontPresetPattern = "^(SYSTEM|SANS|SERIF)$";

    public const string DefaultPrimaryColor = "#111827";
    public const string DefaultSecondaryColor = "#F9FAFB";
    public const string DefaultAccentColor = "#D97706";
    public const string DefaultBackgroundColor = "#FFFFFF";
    public const string DefaultTextColor = "#111827";

    public static readonly IReadOnlyCollection<string> ThemePresets =
    [
        ClassicThemePreset,
        ModernThemePreset,
        CompactThemePreset
    ];

    public static readonly IReadOnlyCollection<string> FontPresets =
    [
        SystemFontPreset,
        SansFontPreset,
        SerifFontPreset
    ];
}

public sealed class AdminBrandingResponse
{
    public long CafeId { get; init; }

    public string CafeName { get; init; } = string.Empty;

    public string? LogoImageUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public string PrimaryColor { get; init; } = string.Empty;

    public string SecondaryColor { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public string BackgroundColor { get; init; } = string.Empty;

    public string TextColor { get; init; } = string.Empty;

    public string? WelcomeTitle { get; init; }

    public string? WelcomeDescription { get; init; }

    public string FontPreset { get; init; } = string.Empty;

    public string ThemePreset { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class AdminBrandingFormModel
{
    private string? _logoImageUrl;
    private string? _coverImageUrl;
    private string? _welcomeTitle;
    private string? _welcomeDescription;

    [StringLength(500, ErrorMessage = "Logo referansı en fazla 500 karakter olabilir.")]
    [SafeBrandingText]
    public string? LogoImageUrl
    {
        get => _logoImageUrl;
        set => _logoImageUrl = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [StringLength(500, ErrorMessage = "Kapak görseli referansı en fazla 500 karakter olabilir.")]
    [SafeBrandingText]
    public string? CoverImageUrl
    {
        get => _coverImageUrl;
        set => _coverImageUrl = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [Required(ErrorMessage = "Primary renk zorunludur.")]
    [RegularExpression(AdminBrandingConstants.HexColorPattern, ErrorMessage = "Primary renk #RRGGBB formatında olmalıdır.")]
    public string PrimaryColor { get; set; } = AdminBrandingConstants.DefaultPrimaryColor;

    [Required(ErrorMessage = "Secondary renk zorunludur.")]
    [RegularExpression(AdminBrandingConstants.HexColorPattern, ErrorMessage = "Secondary renk #RRGGBB formatında olmalıdır.")]
    public string SecondaryColor { get; set; } = AdminBrandingConstants.DefaultSecondaryColor;

    [Required(ErrorMessage = "Accent renk zorunludur.")]
    [RegularExpression(AdminBrandingConstants.HexColorPattern, ErrorMessage = "Accent renk #RRGGBB formatında olmalıdır.")]
    public string AccentColor { get; set; } = AdminBrandingConstants.DefaultAccentColor;

    [Required(ErrorMessage = "Arka plan rengi zorunludur.")]
    [RegularExpression(AdminBrandingConstants.HexColorPattern, ErrorMessage = "Arka plan rengi #RRGGBB formatında olmalıdır.")]
    public string BackgroundColor { get; set; } = AdminBrandingConstants.DefaultBackgroundColor;

    [Required(ErrorMessage = "Metin rengi zorunludur.")]
    [RegularExpression(AdminBrandingConstants.HexColorPattern, ErrorMessage = "Metin rengi #RRGGBB formatında olmalıdır.")]
    public string TextColor { get; set; } = AdminBrandingConstants.DefaultTextColor;

    [StringLength(120, ErrorMessage = "Karşılama başlığı en fazla 120 karakter olabilir.")]
    [SafeBrandingText]
    public string? WelcomeTitle
    {
        get => _welcomeTitle;
        set => _welcomeTitle = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [StringLength(500, ErrorMessage = "Karşılama açıklaması en fazla 500 karakter olabilir.")]
    [SafeBrandingText]
    public string? WelcomeDescription
    {
        get => _welcomeDescription;
        set => _welcomeDescription = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [Required(ErrorMessage = "Font seçimi zorunludur.")]
    [RegularExpression(AdminBrandingConstants.FontPresetPattern, ErrorMessage = "Desteklenen font seçimi kullanılmalıdır.")]
    public string FontPreset { get; set; } = AdminBrandingConstants.SystemFontPreset;

    [Required(ErrorMessage = "Tema seçimi zorunludur.")]
    [RegularExpression(AdminBrandingConstants.ThemePresetPattern, ErrorMessage = "Desteklenen tema seçimi kullanılmalıdır.")]
    public string ThemePreset { get; set; } = AdminBrandingConstants.ClassicThemePreset;

    public bool IsPublished { get; set; }

    public static AdminBrandingFormModel FromBranding(AdminBrandingResponse branding)
    {
        return new AdminBrandingFormModel
        {
            LogoImageUrl = branding.LogoImageUrl,
            CoverImageUrl = branding.CoverImageUrl,
            PrimaryColor = branding.PrimaryColor,
            SecondaryColor = branding.SecondaryColor,
            AccentColor = branding.AccentColor,
            BackgroundColor = branding.BackgroundColor,
            TextColor = branding.TextColor,
            WelcomeTitle = branding.WelcomeTitle,
            WelcomeDescription = branding.WelcomeDescription,
            FontPreset = branding.FontPreset,
            ThemePreset = branding.ThemePreset,
            IsPublished = branding.IsPublished
        };
    }
}

public sealed record AdminUpdateCafeBrandingRequest(
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
    bool IsPublished);

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SafeBrandingTextAttribute : ValidationAttribute
{
    private static readonly string[] UnsafeTokens =
    [
        "<",
        ">",
        "{",
        "}",
        ";",
        "<script",
        "</script",
        "<style",
        "</style",
        "javascript:",
        "expression("
    ];

    public SafeBrandingTextAttribute()
    {
        ErrorMessage = "Değer HTML, CSS veya JavaScript içeriği içeremez.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is string text &&
            !UnsafeTokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
