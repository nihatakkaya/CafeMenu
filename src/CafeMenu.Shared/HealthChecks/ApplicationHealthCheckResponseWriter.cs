using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CafeMenu.Shared.HealthChecks;

public static class ApplicationHealthCheckResponseWriter
{
    private const string HealthyStatus = "Healthy";
    private const string UnhealthyStatus = "Unhealthy";

    public static async Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status == HealthStatus.Healthy
                ? HealthyStatus
                : UnhealthyStatus
        };

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            cancellationToken: context.RequestAborted);
    }
}
