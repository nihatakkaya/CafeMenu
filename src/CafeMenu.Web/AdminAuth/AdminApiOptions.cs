using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminApiOptions
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}
