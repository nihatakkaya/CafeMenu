using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CafeMenu.Shared.HealthChecks;

public static class ApplicationHealthEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApplicationHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/health/live",
            CreateOptions(ApplicationHealthCheckTags.Live));

        endpoints.MapHealthChecks(
            "/health/ready",
            CreateOptions(ApplicationHealthCheckTags.Ready));

        return endpoints;
    }

    private static HealthCheckOptions CreateOptions(string tag)
    {
        return new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(tag),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = ApplicationHealthCheckResponseWriter.WriteAsync
        };
    }
}
