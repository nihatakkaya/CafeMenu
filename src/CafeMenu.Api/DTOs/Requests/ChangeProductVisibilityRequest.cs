using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class ChangeProductVisibilityRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    public bool IsVisible { get; init; }
}
