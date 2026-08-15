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

        if (!_environment.IsDevelopment())
        {
            var configuredLocalRoot = options.LocalRoot ?? string.Empty;
            if (!Path.IsPathFullyQualified(configuredLocalRoot))
            {
                return ValidateOptionsResult.Fail("ImageStorage:LocalRoot must be an absolute filesystem path outside Development.");
            }

            if (IsUnderContentRoot(configuredLocalRoot))
            {
                return ValidateOptionsResult.Fail("ImageStorage:LocalRoot must point to persistent operational storage outside the application source tree outside Development.");
            }
        }

        return ValidateOptionsResult.Success;
    }

    private bool IsUnderContentRoot(string path)
    {
        var contentRoot = Path.GetFullPath(_environment.ContentRootPath);
        var localRoot = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(contentRoot, localRoot);

        return relativePath == "." ||
            (!relativePath.StartsWith("..", StringComparison.Ordinal) &&
                !Path.IsPathFullyQualified(relativePath));
    }
}
