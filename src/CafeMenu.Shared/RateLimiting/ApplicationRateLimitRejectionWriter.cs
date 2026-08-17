using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace CafeMenu.Shared.RateLimiting;

public static class ApplicationRateLimitRejectionWriter
{
    public static ValueTask WriteRejectedResponseAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        return WriteRejectedResponseAsync(context.HttpContext, context.Lease, cancellationToken);
    }

    public static async ValueTask WriteRejectedResponseAsync(
        HttpContext context,
        RateLimitLease lease,
        CancellationToken cancellationToken)
    {
        var response = context.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
        }

        if (!response.HasStarted)
        {
            await response.WriteAsJsonAsync(
                new
                {
                    success = false,
                    message = "Too many requests."
                },
                cancellationToken);
        }
    }
}
