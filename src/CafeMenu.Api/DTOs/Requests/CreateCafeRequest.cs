using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class CreateCafeRequest
{
    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(120, MinimumLength = 2)]
    [RegularExpression("^[a-zA-Z0-9]+(?:-[a-zA-Z0-9]+)*$")]
    public string? Slug { get; init; }
}
