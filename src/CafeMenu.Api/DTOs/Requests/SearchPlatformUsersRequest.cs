using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class SearchPlatformUsersRequest
{
    [Required]
    [StringLength(320, MinimumLength = 2)]
    public string Query { get; init; } = string.Empty;

    [Range(1, 20)]
    public int PageSize { get; init; } = 10;
}
