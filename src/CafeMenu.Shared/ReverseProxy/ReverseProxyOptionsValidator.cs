using System.Net;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.ReverseProxy;

public sealed class ReverseProxyOptionsValidator : IValidateOptions<ReverseProxyOptions>
{
    public ValidateOptionsResult Validate(string? name, ReverseProxyOptions options)
    {
        var failures = new List<string>();

        if (options.ForwardLimit < 1)
        {
            failures.Add("ReverseProxy:ForwardLimit must be greater than or equal to 1.");
        }

        ValidateKnownProxies(options.KnownProxies, failures);
        ValidateKnownIPNetworks(options.KnownIPNetworks, failures);

        if (options.Enabled &&
            options.KnownProxies.Length == 0 &&
            options.KnownIPNetworks.Length == 0)
        {
            failures.Add("ReverseProxy requires at least one trusted KnownProxy or KnownIPNetwork when Enabled is true.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateKnownProxies(IEnumerable<string> knownProxies, ICollection<string> failures)
    {
        foreach (var knownProxy in knownProxies)
        {
            if (string.IsNullOrWhiteSpace(knownProxy) ||
                !IPAddress.TryParse(knownProxy, out _))
            {
                failures.Add($"ReverseProxy:KnownProxies contains an invalid IP address: '{knownProxy}'.");
            }
        }
    }

    private static void ValidateKnownIPNetworks(IEnumerable<string> knownIPNetworks, ICollection<string> failures)
    {
        foreach (var knownIPNetwork in knownIPNetworks)
        {
            if (!ReverseProxyCidrParser.TryParse(knownIPNetwork, out _))
            {
                failures.Add($"ReverseProxy:KnownIPNetworks contains an invalid CIDR network: '{knownIPNetwork}'.");
            }
        }
    }
}
