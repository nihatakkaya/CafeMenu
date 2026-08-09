using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class AssignCafeOwnerRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    [Range(1, long.MaxValue)]
    public long AppUserId { get; init; }
}
