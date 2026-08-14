using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminSessionOptions
{
    public const string SectionName = "AdminSession";

    [Required]
    [StringLength(32)]
    public string Provider { get; set; } = AdminSessionProvider.Memory;

    [Required]
    [StringLength(200, MinimumLength = 4)]
    public string KeyPrefix { get; set; } = "cafemenu:admin-session:";

    [StringLength(1024)]
    public string? RedisConnectionString { get; set; }

    [Range(1, 3600)]
    public int MinimumCacheTtlSeconds { get; set; } = 1;
}
