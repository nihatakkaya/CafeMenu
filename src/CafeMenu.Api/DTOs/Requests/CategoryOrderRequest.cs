using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class CategoryOrderRequest
{
    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }
}
