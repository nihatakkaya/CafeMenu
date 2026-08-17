using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class CompleteUserSetupRequest
{
    [Required]
    [StringLength(256, MinimumLength = 32)]
    public string Token { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string ConfirmPassword { get; init; } = string.Empty;
}
