using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.HostFiltering;

public sealed class AllowedHostsOptionsValidator : IValidateOptions<HostFilteringOptions>
{
    private const string OptionName = "AllowedHosts";

    private readonly IHostEnvironment _environment;

    public AllowedHostsOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, HostFilteringOptions options)
    {
        if (_environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        var allowedHosts = options.AllowedHosts
            .SelectMany(host => host.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .ToArray();

        if (allowedHosts.Length == 0)
        {
            failures.Add($"{OptionName} must contain at least one explicit host outside Development.");
            return ValidateOptionsResult.Fail(failures);
        }

        foreach (var allowedHost in allowedHosts)
        {
            ValidateAllowedHostEntry(allowedHost, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateAllowedHostEntry(string allowedHost, ICollection<string> failures)
    {
        if (allowedHost == "*")
        {
            failures.Add($"{OptionName} must not use unrestricted wildcard '*' outside Development.");
            return;
        }

        if (allowedHost.Contains("://", StringComparison.Ordinal) ||
            allowedHost.Contains('/') ||
            allowedHost.Contains('\\') ||
            allowedHost.Contains('?') ||
            allowedHost.Contains('#'))
        {
            failures.Add($"{OptionName} entry '{allowedHost}' must be a host name only, without scheme, path, query or fragment.");
            return;
        }

        if (allowedHost.Contains(':'))
        {
            failures.Add($"{OptionName} entry '{allowedHost}' must not include a port.");
            return;
        }

        var hostToValidate = allowedHost;
        if (allowedHost.StartsWith("*.", StringComparison.Ordinal))
        {
            hostToValidate = allowedHost[2..];
        }
        else if (allowedHost.Contains('*'))
        {
            failures.Add($"{OptionName} entry '{allowedHost}' contains an unsupported wildcard.");
            return;
        }

        if (string.IsNullOrWhiteSpace(hostToValidate) ||
            hostToValidate.StartsWith(".", StringComparison.Ordinal) ||
            hostToValidate.EndsWith(".", StringComparison.Ordinal) ||
            Uri.CheckHostName(hostToValidate) == UriHostNameType.Unknown)
        {
            failures.Add($"{OptionName} entry '{allowedHost}' is not a valid host name.");
        }
    }
}
