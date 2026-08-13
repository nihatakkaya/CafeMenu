using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Storage;

public sealed class ImageStorageOptionsValidator : IValidateOptions<ImageStorageOptions>
{
    private readonly IWebHostEnvironment _environment;

    public ImageStorageOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, ImageStorageOptions options)
    {
        if (!string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("ImageStorage:Provider must be Local in this version.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return ValidateOptionsResult.Fail("ImageStorage:PublicBaseUrl is required.");
        }

        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var publicBaseUri) ||
            (publicBaseUri.Scheme != Uri.UriSchemeHttp && publicBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail("ImageStorage:PublicBaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (!_environment.IsDevelopment() && publicBaseUri.IsLoopback)
        {
            return ValidateOptionsResult.Fail("ImageStorage:PublicBaseUrl must not point to localhost outside Development.");
        }

        if (!_environment.IsDevelopment() && string.IsNullOrWhiteSpace(options.LocalRoot))
        {
            return ValidateOptionsResult.Fail("ImageStorage:LocalRoot is required when the Local provider is used outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
