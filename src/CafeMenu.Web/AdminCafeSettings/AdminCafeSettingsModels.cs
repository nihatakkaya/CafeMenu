using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminCafeSettings;

public sealed class AdminCafeSettingsResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsPublished { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class AdminCafeSettingsFormModel
{
    private string? _slug;

    [Required(ErrorMessage = "Cafe adı zorunludur.")]
    [StringLength(160, MinimumLength = 2, ErrorMessage = "Cafe adı 2 ile 160 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(120, MinimumLength = 2, ErrorMessage = "Slug 2 ile 120 karakter arasında olmalıdır.")]
    [RegularExpression("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$", ErrorMessage = "Slug yalnız harf, rakam ve tek tire ayırıcıları içerebilir.")]
    public string? Slug
    {
        get => _slug;
        set => _slug = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static AdminCafeSettingsFormModel FromSettings(AdminCafeSettingsResponse settings)
    {
        return new AdminCafeSettingsFormModel
        {
            Name = settings.Name,
            Slug = settings.Slug
        };
    }
}

public sealed record AdminUpdateCafeSettingsRequest(
    string Name,
    string? Slug);

public sealed class AdminCafePublicationActionFormModel
{
    public bool IsPublished { get; set; }
}

public sealed record AdminChangeCafePublicationRequest(bool IsPublished);
