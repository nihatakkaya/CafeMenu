using CafeMenu.Shared.HealthChecks;
using CafeMenu.Web.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CafeMenu.Web.Configuration;

public static class HealthChecksServiceCollectionExtensions
{
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: [ApplicationHealthCheckTags.Live])
            .AddCheck<AdminSessionReadinessHealthCheck>(
                "admin_session",
                tags: [ApplicationHealthCheckTags.Ready],
                timeout: ReadinessTimeout);

        return services;
    }
}
