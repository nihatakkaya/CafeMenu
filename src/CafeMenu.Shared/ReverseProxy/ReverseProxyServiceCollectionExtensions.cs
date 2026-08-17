using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.ReverseProxy;

public static class ReverseProxyServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ReverseProxyOptions>()
            .Bind(configuration.GetSection(ReverseProxyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<ReverseProxyOptions>, ReverseProxyOptionsValidator>();
        services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ReverseProxyForwardedHeadersOptionsSetup>();

        return services;
    }
}
