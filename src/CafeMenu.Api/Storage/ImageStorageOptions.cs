using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Storage;

public sealed class ImageStorageOptions
{
    public const string SectionName = "ImageStorage";

    public const long DefaultMaxFileSizeBytes = 5 * 1024 * 1024;

    [Required]
    public string Provider { get; init; } = "Local";

    public string? LocalRoot { get; init; }

    [Required]
    public string PublicBaseUrl { get; init; } = string.Empty;

    [Range(1, 20 * 1024 * 1024)]
    public long MaxFileSizeBytes { get; init; } = DefaultMaxFileSizeBytes;
}
