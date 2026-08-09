using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class ReorderCategoriesRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<CategoryOrderRequest> Categories { get; init; } = Array.Empty<CategoryOrderRequest>();
}
