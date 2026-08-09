using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.PublicMenu;

public sealed class PublicMenuApiOptions
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}
