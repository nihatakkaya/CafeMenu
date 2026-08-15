using CafeMenu.Api.HealthChecks;
using CafeMenu.Shared.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CafeMenu.Api.Configuration;

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
            .AddCheck<PostgreSqlReadinessHealthCheck>(
                "postgresql",
                tags: [ApplicationHealthCheckTags.Ready],
                timeout: ReadinessTimeout);

        return services;
    }
}
