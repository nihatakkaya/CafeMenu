using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.HostFiltering;

public static class HostFilteringServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationHostFiltering(this IServiceCollection services)
    {
        services.PostConfigure<HostFilteringOptions>(options =>
        {
            options.AllowEmptyHosts = false;
            options.IncludeFailureMessage = false;
        });

        services.AddOptions<HostFilteringOptions>()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<HostFilteringOptions>, AllowedHostsOptionsValidator>();

        return services;
    }
}
