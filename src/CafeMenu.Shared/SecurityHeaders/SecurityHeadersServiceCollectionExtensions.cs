using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Shared.SecurityHeaders;

public static class SecurityHeadersServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationSecurityHeaders(this IServiceCollection services)
    {
        services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);

        return services;
    }
}
