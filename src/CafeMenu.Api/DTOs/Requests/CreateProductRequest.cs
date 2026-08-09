using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class CreateProductRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "9999999999")]
    public decimal Price { get; init; }

    [StringLength(500)]
    [Url]
    public string? ImageUrl { get; init; }

    public bool IsAvailable { get; init; } = true;

    public bool IsVisible { get; init; } = true;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }
}
