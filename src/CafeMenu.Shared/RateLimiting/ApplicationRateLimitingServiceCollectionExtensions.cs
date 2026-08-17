using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.RateLimiting;

public static class ApplicationRateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApplicationRateLimitingOptions>()
            .Bind(configuration.GetSection(ApplicationRateLimitingOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ApplicationRateLimitingOptions>, ApplicationRateLimitingOptionsValidator>();
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, ApplicationRateLimiterOptionsSetup>();
        services.AddRateLimiter();

        return services;
    }
}
