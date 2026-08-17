using CafeMenu.Web.AdminAuth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.HealthChecks;

public sealed class AdminSessionReadinessHealthCheck : IHealthCheck
{
    private const string ProbeKey = "cafemenu:health:admin-session";

    private readonly IDistributedCache _distributedCache;
    private readonly IOptions<AdminSessionOptions> _options;

    public AdminSessionReadinessHealthCheck(
        IDistributedCache distributedCache,
        IOptions<AdminSessionOptions> options)
    {
        _distributedCache = distributedCache;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!AdminSessionProvider.IsRedis(_options.Value.Provider))
        {
            return HealthCheckResult.Healthy();
        }

        try
        {
            _ = await _distributedCache.GetAsync(ProbeKey, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
