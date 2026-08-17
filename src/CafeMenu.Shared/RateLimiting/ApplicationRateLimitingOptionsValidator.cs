using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.RateLimiting;

public sealed class ApplicationRateLimitingOptionsValidator : IValidateOptions<ApplicationRateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationRateLimitingOptions options)
    {
        var failures = new List<string>();

        ValidatePolicy(ApplicationRateLimitPolicyNames.Login, options.Login, failures);
        ValidatePolicy(ApplicationRateLimitPolicyNames.Refresh, options.Refresh, failures);
        ValidatePolicy(ApplicationRateLimitPolicyNames.AccountSetup, options.AccountSetup, failures);
        ValidatePolicy(ApplicationRateLimitPolicyNames.PlatformUserSetup, options.PlatformUserSetup, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidatePolicy(
        string policyName,
        RateLimitPolicyOptions policyOptions,
        ICollection<string> failures)
    {
        if (policyOptions.PermitLimit < 1)
        {
            failures.Add($"RateLimiting:{policyName}:PermitLimit must be greater than or equal to 1.");
        }

        if (policyOptions.WindowSeconds < 1)
        {
            failures.Add($"RateLimiting:{policyName}:WindowSeconds must be greater than or equal to 1.");
        }
    }
}
