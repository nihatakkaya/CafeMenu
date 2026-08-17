using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.Configuration;

public static class WebDataProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDataProtection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<WebDataProtectionOptions>()
            .Bind(configuration.GetSection(WebDataProtectionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<WebDataProtectionOptions>, WebDataProtectionOptionsValidator>();

        var configuredOptions = configuration
            .GetSection(WebDataProtectionOptions.SectionName)
            .Get<WebDataProtectionOptions>()
            ?? new WebDataProtectionOptions();

        var applicationName = string.IsNullOrWhiteSpace(configuredOptions.ApplicationName)
            ? "CafeMenu.Web"
            : configuredOptions.ApplicationName.Trim();

        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(applicationName);

        if (WebDataProtectionPath.TryNormalizeAbsolutePath(configuredOptions.KeyRingPath, out var keyRingPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }

        return services;
    }
}
