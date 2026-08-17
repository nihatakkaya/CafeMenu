using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.RateLimiting;

public sealed class ApplicationAccountSetupPostRateLimitingMiddleware
{
    private const string UnknownClientPartitionKey = "unknown-client";

    private readonly RequestDelegate _next;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;

    public ApplicationAccountSetupPostRateLimitingMiddleware(
        RequestDelegate next,
        IOptions<ApplicationRateLimitingOptions> options)
    {
        _next = next;
        var accountSetupOptions = options.Value.AccountSetup;
        _limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitPartition.GetFixedWindowLimiter(
                GetClientIpPartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = accountSetupOptions.PermitLimit,
                    Window = TimeSpan.FromSeconds(accountSetupOptions.WindowSeconds),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldApply(context))
        {
            await _next(context);
            return;
        }

        using var lease = await _limiter.AcquireAsync(
            context,
            permitCount: 1,
            context.RequestAborted);

        if (lease.IsAcquired)
        {
            await _next(context);
            return;
        }

        await ApplicationRateLimitRejectionWriter.WriteRejectedResponseAsync(
            context,
            lease,
            context.RequestAborted);
    }

    private static bool ShouldApply(HttpContext context)
    {
        return HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/account/setup", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClientIpPartitionKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownClientPartitionKey;
    }
}

public static class ApplicationAccountSetupPostRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseApplicationAccountSetupPostRateLimiting(
        this IApplicationBuilder applicationBuilder)
    {
        return applicationBuilder.UseMiddleware<ApplicationAccountSetupPostRateLimitingMiddleware>();
    }
}
