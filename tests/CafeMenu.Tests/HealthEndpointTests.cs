extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using CafeMenu.Api.Data;
using CafeMenu.Shared.SecurityHeaders;
using CafeMenuWeb::CafeMenu.Web.AccountSetup;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task ApiLive_ShouldReturnHealthyWithoutRequiringAuthentication()
    {
        await using var factory = new ApiHealthFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task ApiReady_ShouldReturnHealthyWhenDatabaseCanConnect()
    {
        await using var factory = new ApiHealthFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
    }

    [Fact]
    public async Task ApiReady_ShouldReturnServiceUnavailableWhenDatabaseCannotConnect()
    {
        await using var factory = new ApiHealthFactory(useUnavailableDatabase: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", GetHealthStatus(content));
        Assert.DoesNotContain("127.0.0.1", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cafemenu_unavailable", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiLive_ShouldStayHealthyWhenDatabaseCannotConnect()
    {
        await using var factory = new ApiHealthFactory(useUnavailableDatabase: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
    }

    [Fact]
    public async Task ApiHealthEndpoints_ShouldNotBeRateLimitedByAuthenticationPolicies()
    {
        await using var factory = new ApiHealthFactory(
            configurationOverrides: new Dictionary<string, string?>
            {
                ["RateLimiting:Login:PermitLimit"] = "1",
                ["RateLimiting:Login:WindowSeconds"] = "60"
            });
        using var client = factory.CreateClient();

        _ = await client.PostAsync(
            "/Authentication/Login",
            JsonContent("""{"email":"limited@example.local","password":"wrong-password"}"""));
        _ = await client.PostAsync(
            "/Authentication/Login",
            JsonContent("""{"email":"limited@example.local","password":"wrong-password"}"""));

        using var response = await client.GetAsync("/health/live");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
    }

    [Fact]
    public async Task WebLive_ShouldReturnHealthyWithoutRequiringAuthentication()
    {
        await using var factory = new WebHealthFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/health/live");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task WebReady_ShouldReturnHealthyForDevelopmentMemoryAdminSessionProvider()
    {
        await using var factory = new WebHealthFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
    }

    [Fact]
    public async Task WebReady_ShouldReturnHealthyWhenRedisProviderCacheIsHealthy()
    {
        await using var factory = new WebHealthFactory(
            provider: AdminSessionProvider.Redis,
            distributedCache: new HealthyDistributedCache());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
    }

    [Fact]
    public async Task WebReady_ShouldReturnServiceUnavailableWhenRedisProviderCacheIsUnavailable()
    {
        await using var factory = new WebHealthFactory(
            provider: AdminSessionProvider.Redis,
            distributedCache: new UnavailableDistributedCache());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", GetHealthStatus(content));
        Assert.DoesNotContain("redis-secret", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localhost:6379", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WebLive_ShouldStayHealthyWhenRedisProviderCacheIsUnavailable()
    {
        await using var factory = new WebHealthFactory(
            provider: AdminSessionProvider.Redis,
            distributedCache: new UnavailableDistributedCache());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        await AssertHealthResponseAsync(response, HttpStatusCode.OK, "Healthy");
    }

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task AssertHealthResponseAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedStatus)
    {
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedStatus, GetHealthStatus(content));
        Assert.DoesNotContain("connection", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetHealthStatus(string content)
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Single(root.EnumerateObject());

        return root.GetProperty("status").GetString() ?? string.Empty;
    }

    private static void AssertBaselineSecurityHeaders(HttpResponseMessage response)
    {
        AssertHeader(
            response,
            ApplicationSecurityHeaders.XContentTypeOptionsHeaderName,
            ApplicationSecurityHeaders.XContentTypeOptionsValue);
        AssertHeader(
            response,
            ApplicationSecurityHeaders.ReferrerPolicyHeaderName,
            ApplicationSecurityHeaders.ReferrerPolicyValue);
    }

    private static void AssertHeader(HttpResponseMessage response, string headerName, string expectedValue)
    {
        Assert.True(response.Headers.TryGetValues(headerName, out var values), $"{headerName} was missing.");
        Assert.Equal(expectedValue, Assert.Single(values));
    }

    private sealed class ApiHealthFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly string _databaseName = $"cafemenu_health_tests_{Guid.NewGuid():N}";
        private readonly bool _useUnavailableDatabase;

        public ApiHealthFactory(
            bool useUnavailableDatabase = false,
            IReadOnlyDictionary<string, string?>? configurationOverrides = null)
        {
            _useUnavailableDatabase = useUnavailableDatabase;
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=cafemenu_test;Username=test;Password=test",
                    ["ImageStorage:Provider"] = "Local",
                    ["ImageStorage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "cafemenu-tests-media", _databaseName),
                    ["ImageStorage:PublicBaseUrl"] = "http://localhost/media",
                    ["ImageStorage:MaxFileSizeBytes"] = "5242880"
                };

                foreach (var item in _configurationOverrides)
                {
                    configuration[item.Key] = item.Value;
                }

                configurationBuilder.AddInMemoryCollection(configuration);
            });
            builder.ConfigureTestServices(services =>
            {
                RemoveDbContextRegistrations(services);

                if (_useUnavailableDatabase)
                {
                    services.AddDbContext<CafeMenuDbContext>(options =>
                        options.UseNpgsql("Host=127.0.0.1;Port=1;Database=cafemenu_unavailable;Username=test;Password=secret;Timeout=1"));
                    return;
                }

                services.AddDbContext<CafeMenuDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        }

        private static void RemoveDbContextRegistrations(IServiceCollection services)
        {
            var descriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(CafeMenuDbContext) ||
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    descriptor.ServiceType == typeof(DbContextOptions<CafeMenuDbContext>) ||
                    descriptor.ServiceType.Name.Contains("DbContextOptionsConfiguration", StringComparison.Ordinal))
                .ToArray();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
        }
    }

    private sealed class WebHealthFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IDistributedCache? _distributedCache;
        private readonly string _provider;

        public WebHealthFactory(
            string provider = AdminSessionProvider.Memory,
            IDistributedCache? distributedCache = null)
        {
            _provider = provider;
            _distributedCache = distributedCache;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AdminSession:Provider"] = _provider,
                    ["AdminSession:RedisConnectionString"] = "localhost:6379,abortConnect=false"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountSetupApiClient>();
                services.AddSingleton<IAccountSetupApiClient>(new StubAccountSetupApiClient());
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                if (_distributedCache is not null)
                {
                    services.RemoveAll<IDistributedCache>();
                    services.AddSingleton(_distributedCache);
                }

                var keyDirectory = new DirectoryInfo(Path.Combine(
                    Path.GetTempPath(),
                    "cafemenu-health-web-data-protection",
                    Guid.NewGuid().ToString("N")));
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }
    }

    private sealed class HealthyDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key)
        {
            return null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult<byte[]?>(null);
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class UnavailableDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key)
        {
            throw CreateException();
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            throw CreateException();
        }

        public void Refresh(string key)
        {
            throw CreateException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw CreateException();
        }

        public void Remove(string key)
        {
            throw CreateException();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            throw CreateException();
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw CreateException();
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            throw CreateException();
        }

        private static InvalidOperationException CreateException()
        {
            return new InvalidOperationException("redis-secret localhost:6379 failure");
        }
    }

    private sealed class StubAccountSetupApiClient : IAccountSetupApiClient
    {
        public Task<AccountSetupResult> CompleteUserSetupAsync(
            AccountSetupRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AccountSetupResult.Failure(AccountSetupStatus.InvalidToken));
        }
    }

    private sealed class StubPublicMenuApiClient : IPublicMenuApiClient
    {
        public Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(PublicMenuRequestResult.NotFound());
        }

        public Task<PublicProductDetailRequestResult> GetProductDetailAsync(
            string slug,
            long productId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PublicProductDetailRequestResult.NotFound());
        }
    }
}
