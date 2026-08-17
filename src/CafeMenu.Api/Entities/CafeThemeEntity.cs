namespace CafeMenu.Api.Entities;

public sealed class CafeThemeEntity
{
    public long Id { get; set; }

    public long CafeId { get; set; }

    public string PrimaryColor { get; set; } = string.Empty;

    public string SecondaryColor { get; set; } = string.Empty;

    public string AccentColor { get; set; } = string.Empty;

    public string BackgroundColor { get; set; } = string.Empty;

    public string TextColor { get; set; } = string.Empty;

    public string? WelcomeTitle { get; set; }

    public string? WelcomeDescription { get; set; }

    public string FontPreset { get; set; } = string.Empty;

    public string ThemePreset { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public CafeEntity Cafe { get; set; } = null!;
}
