using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Configuration;

public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public const int MaximumRetryCount = 10;
    public const int MaximumRetryDelaySeconds = 60;

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        if (!options.Retry.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.Retry.MaxRetryCount < 1)
        {
            failures.Add("Database:Retry:MaxRetryCount must be greater than or equal to 1 when retry is enabled.");
        }

        if (options.Retry.MaxRetryCount > MaximumRetryCount)
        {
            failures.Add($"Database:Retry:MaxRetryCount must be less than or equal to {MaximumRetryCount}.");
        }

        if (options.Retry.MaxRetryDelaySeconds < 1)
        {
            failures.Add("Database:Retry:MaxRetryDelaySeconds must be greater than or equal to 1 when retry is enabled.");
        }

        if (options.Retry.MaxRetryDelaySeconds > MaximumRetryDelaySeconds)
        {
            failures.Add($"Database:Retry:MaxRetryDelaySeconds must be less than or equal to {MaximumRetryDelaySeconds}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
