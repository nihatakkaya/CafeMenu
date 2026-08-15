using Microsoft.Extensions.Options;

namespace CafeMenu.Web.Configuration;

internal static class HttpBaseUrlOptionsValidator
{
    public static ValidateOptionsResult Validate(
        string optionName,
        string? baseUrl,
        IHostEnvironment environment)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            failures.Add($"{optionName}:BaseUrl is required.");
            return ValidateOptionsResult.Fail(failures);
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            failures.Add($"{optionName}:BaseUrl must be an absolute URI.");
            return ValidateOptionsResult.Fail(failures);
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add($"{optionName}:BaseUrl must use HTTP or HTTPS.");
        }

        if (!environment.IsDevelopment())
        {
            if (IsLocalhostOrLoopback(uri))
            {
                failures.Add($"{optionName}:BaseUrl must not point to localhost or loopback outside Development.");
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                failures.Add($"{optionName}:BaseUrl must use HTTPS outside Development.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsLocalhostOrLoopback(Uri uri)
    {
        return uri.IsLoopback ||
            string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}
