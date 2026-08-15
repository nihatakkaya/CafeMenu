using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.ReverseProxy;

public static class ReverseProxyApplicationBuilderExtensions
{
    public static IApplicationBuilder UseConfiguredForwardedHeaders(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<ReverseProxyOptions>>().Value;

        if (options.Enabled)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }
}
