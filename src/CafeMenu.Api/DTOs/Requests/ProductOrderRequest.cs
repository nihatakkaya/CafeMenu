using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class ProductOrderRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }
}
