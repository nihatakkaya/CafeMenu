namespace CafeMenu.Shared.RateLimiting;

public sealed class ApplicationRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions Login { get; init; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions Refresh { get; init; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions AccountSetup { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 300
    };

    public RateLimitPolicyOptions PlatformUserSetup { get; init; } = new()
    {
        PermitLimit = 20,
        WindowSeconds = 60
    };
}
