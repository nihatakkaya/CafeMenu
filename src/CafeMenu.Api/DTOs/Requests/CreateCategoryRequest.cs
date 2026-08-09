using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class CreateCategoryRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [StringLength(500)]
    [Url]
    public string? ImageUrl { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }

    public bool IsVisible { get; init; } = true;
}
