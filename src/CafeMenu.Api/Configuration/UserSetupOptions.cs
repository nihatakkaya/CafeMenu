using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Configuration;

public sealed class UserSetupOptions
{
    public const string SectionName = "UserSetup";

    [Range(1, 720)]
    public int TokenExpirationHours { get; init; } = 24;

    public TimeSpan TokenExpiration => TimeSpan.FromHours(TokenExpirationHours);
}
