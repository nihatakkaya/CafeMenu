using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AdminQr;

public sealed class AdminQrOptionsValidator : IValidateOptions<PublicMenuQrOptions>
{
    private readonly IWebHostEnvironment _environment;

    public AdminQrOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, PublicMenuQrOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("PublicMenu:BaseUrl is required for QR code generation.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail("PublicMenu:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (!_environment.IsDevelopment() && IsLocalhost(uri))
        {
            return ValidateOptionsResult.Fail("PublicMenu:BaseUrl must not point to localhost outside Development.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsLocalhost(Uri uri)
    {
        return uri.IsLoopback ||
            string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}
