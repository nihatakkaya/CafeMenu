namespace CafeMenu.Shared.RateLimiting;

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; }
}
