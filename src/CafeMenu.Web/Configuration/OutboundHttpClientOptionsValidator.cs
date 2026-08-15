using Microsoft.Extensions.Options;

namespace CafeMenu.Web.Configuration;

public sealed class OutboundHttpClientOptionsValidator : IValidateOptions<OutboundHttpClientOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboundHttpClientOptions options)
    {
        if (options.DefaultTimeoutSeconds < OutboundHttpClientOptions.MinimumTimeoutSeconds ||
            options.DefaultTimeoutSeconds > OutboundHttpClientOptions.MaximumTimeoutSeconds)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboundHttpClientOptions.SectionName}:DefaultTimeoutSeconds must be between " +
                $"{OutboundHttpClientOptions.MinimumTimeoutSeconds} and " +
                $"{OutboundHttpClientOptions.MaximumTimeoutSeconds} seconds.");
        }

        return ValidateOptionsResult.Success;
    }
}
