using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}
