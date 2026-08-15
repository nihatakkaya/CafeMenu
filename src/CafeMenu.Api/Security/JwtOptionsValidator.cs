using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Security;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private static readonly string[] ProductionUnsafeSigningKeys =
    [
        "replace_with_local_development_key_at_least_32_chars",
        "development_placeholder_signing_key_change_me_32_chars_min"
    ];

    private readonly IWebHostEnvironment _environment;

    public JwtOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("Jwt:SigningKey is required.");
        }
        else if (options.SigningKey.Length < JwtOptions.MinimumSigningKeyLength)
        {
            failures.Add($"Jwt:SigningKey must be at least {JwtOptions.MinimumSigningKeyLength} characters.");
        }

        if (options.AccessTokenMinutes < 1 || options.AccessTokenMinutes > 1440)
        {
            failures.Add("Jwt:AccessTokenMinutes must be between 1 and 1440.");
        }

        if (options.RefreshTokenDays < 1 || options.RefreshTokenDays > 60)
        {
            failures.Add("Jwt:RefreshTokenDays must be between 1 and 60.");
        }

        if (!_environment.IsDevelopment() &&
            ProductionUnsafeSigningKeys.Contains(options.SigningKey, StringComparer.Ordinal))
        {
            failures.Add("Jwt:SigningKey must not use the committed development placeholder outside Development.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
