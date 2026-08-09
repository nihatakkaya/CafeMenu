using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class ReorderProductsRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<ProductOrderRequest> Products { get; init; } = Array.Empty<ProductOrderRequest>();
}
