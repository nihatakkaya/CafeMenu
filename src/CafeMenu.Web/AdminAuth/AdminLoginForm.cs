using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminLoginForm
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }
}
