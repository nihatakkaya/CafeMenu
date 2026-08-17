using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class ChangeProductPublicationRequest
{
    [Range(1, long.MaxValue)]
    public long CafeId { get; init; }

    public bool IsPublished { get; init; }
}
