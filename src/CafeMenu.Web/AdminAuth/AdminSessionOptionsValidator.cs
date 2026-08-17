using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminSessionOptionsValidator : IValidateOptions<AdminSessionOptions>
{
    private readonly IWebHostEnvironment _environment;

    public AdminSessionOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AdminSessionOptions options)
    {
        var failures = new List<string>();

        if (!AdminSessionProvider.IsMemory(options.Provider) &&
            !AdminSessionProvider.IsRedis(options.Provider))
        {
            failures.Add("AdminSession:Provider must be Memory or Redis.");
        }

        if (!_environment.IsDevelopment() && AdminSessionProvider.IsMemory(options.Provider))
        {
            failures.Add(AdminAuthServiceCollectionExtensions.MemoryStoreProductionGuardMessage);
        }

        if (AdminSessionProvider.IsRedis(options.Provider) &&
            string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            failures.Add("AdminSession:RedisConnectionString is required when AdminSession:Provider is Redis.");
        }

        if (options.MinimumCacheTtlSeconds <= 0)
        {
            failures.Add("AdminSession:MinimumCacheTtlSeconds must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
