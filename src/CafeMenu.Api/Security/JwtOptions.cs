using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumSigningKeyLength = 32;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(MinimumSigningKeyLength)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 60)]
    public int RefreshTokenDays { get; init; } = 14;
}
