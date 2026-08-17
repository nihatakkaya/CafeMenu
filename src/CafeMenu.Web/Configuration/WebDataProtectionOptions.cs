namespace CafeMenu.Web.Configuration;

public sealed class WebDataProtectionOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; init; } = "CafeMenu.Web";

    public string KeyRingPath { get; init; } = string.Empty;
}
