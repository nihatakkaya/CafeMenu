using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class LogoutRequest
{
    [Required]
    [StringLength(512)]
    public string RefreshToken { get; init; } = string.Empty;
}
