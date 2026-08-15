using Microsoft.Extensions.Options;

namespace CafeMenu.Web.Configuration;

public sealed class WebDataProtectionOptionsValidator : IValidateOptions<WebDataProtectionOptions>
{
    private readonly IWebHostEnvironment _environment;

    public WebDataProtectionOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, WebDataProtectionOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            failures.Add("DataProtection:ApplicationName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyRingPath))
        {
            if (!_environment.IsDevelopment())
            {
                failures.Add("DataProtection:KeyRingPath is required outside Development.");
            }
        }
        else if (!WebDataProtectionPath.TryNormalizeAbsolutePath(options.KeyRingPath, out _))
        {
            failures.Add("DataProtection:KeyRingPath must be a valid absolute filesystem path.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
