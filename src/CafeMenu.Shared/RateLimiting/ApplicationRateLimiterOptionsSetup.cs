using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CafeMenu.Shared.RateLimiting;

public sealed class ApplicationRateLimiterOptionsSetup : IConfigureOptions<RateLimiterOptions>
{
    private const string UnknownClientPartitionKey = "unknown-client";

    private readonly IOptions<ApplicationRateLimitingOptions> _options;

    public ApplicationRateLimiterOptionsSetup(IOptions<ApplicationRateLimitingOptions> options)
    {
        _options = options;
    }

    public void Configure(RateLimiterOptions options)
    {
        var rateLimitingOptions = _options.Value;

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = ApplicationRateLimitRejectionWriter.WriteRejectedResponseAsync;

        options.AddPolicy(
            ApplicationRateLimitPolicyNames.Login,
            context => CreateIpPartition(context, rateLimitingOptions.Login));
        options.AddPolicy(
            ApplicationRateLimitPolicyNames.Refresh,
            context => CreateIpPartition(context, rateLimitingOptions.Refresh));
        options.AddPolicy(
            ApplicationRateLimitPolicyNames.AccountSetup,
            context => CreatePostIpPartition(context, rateLimitingOptions.AccountSetup));
        options.AddPolicy(
            ApplicationRateLimitPolicyNames.PlatformUserSetup,
            context => CreateUserOrIpPartition(context, rateLimitingOptions.PlatformUserSetup));
    }

    private static RateLimitPartition<string> CreateIpPartition(
        HttpContext context,
        RateLimitPolicyOptions options)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            GetClientIpPartitionKey(context),
            _ => CreateFixedWindowLimiterOptions(options));
    }

    private static RateLimitPartition<string> CreatePostIpPartition(
        HttpContext context,
        RateLimitPolicyOptions options)
    {
        if (!ShouldApplyAccountSetupLimit(context))
        {
            return RateLimitPartition.GetNoLimiter(GetClientIpPartitionKey(context));
        }

        return CreateIpPartition(context, options);
    }

    private static bool ShouldApplyAccountSetupLimit(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        return context.Request.Path.Equals("/account/setup", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.Equals("/PlatformUser/CompleteUserSetup", StringComparison.OrdinalIgnoreCase);
    }

    private static RateLimitPartition<string> CreateUserOrIpPartition(
        HttpContext context,
        RateLimitPolicyOptions options)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            GetAuthenticatedUserPartitionKey(context) ?? GetClientIpPartitionKey(context),
            _ => CreateFixedWindowLimiterOptions(options));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowLimiterOptions(
        RateLimitPolicyOptions options)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };
    }

    private static string GetClientIpPartitionKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownClientPartitionKey;
    }

    private static string? GetAuthenticatedUserPartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = context.User.FindFirstValue("app_user_id")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(userId)
            ? null
            : $"user:{userId}";
    }

}
