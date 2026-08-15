extern alias CafeMenuWeb;

using System.Net;
using CafeMenu.Api.Data;
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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

[Collection(EnvironmentMutatingTestCollection.Name)]
public sealed class FinalProductionStartupConfigurationTests
{
    [Fact]
    public async Task ApiProductionStartup_ShouldSucceedWithValidDeterministicConfiguration()
    {
        await using var factory = new ApiProductionConfigurationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ApiProductionStartup_ShouldFailFastWithMissingDatabaseConnectionString()
    {
        using var factory = new ApiProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = string.Empty
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "Connection string 'DefaultConnection' is not configured.");
    }

    [Theory]
    [InlineData("", "Jwt:SigningKey is required.")]
    [InlineData("short", "Jwt:SigningKey must be at least")]
    [InlineData("replace_with_local_development_key_at_least_32_chars", "committed development placeholder")]
    [InlineData("development_placeholder_signing_key_change_me_32_chars_min", "committed development placeholder")]
    public void ApiProductionStartup_ShouldFailFastWithUnsafeJwtSigningKey(
        string signingKey,
        string expectedMessage)
    {
        using var factory = new ApiProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = signingKey
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            expectedMessage);
    }

    [Fact]
    public void ApiProductionStartup_ShouldFailFastWithWildcardAllowedHosts()
    {
        using var factory = new ApiProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*"
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "AllowedHosts must not use unrestricted wildcard");
    }

    [Fact]
    public void ApiProductionStartup_ShouldFailFastWithReverseProxyEnabledWithoutTrustedProxy()
    {
        using var factory = new ApiProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["ReverseProxy:Enabled"] = "true",
            ["ReverseProxy:KnownProxies"] = string.Empty,
            ["ReverseProxy:KnownIPNetworks"] = string.Empty
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "ReverseProxy requires at least one trusted KnownProxy or KnownIPNetwork");
    }

    [Fact]
    public async Task ApiProductionStartup_ShouldNotRequireDatabaseConnectivityForConfigValidation()
    {
        await using var factory = new ApiProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=127.0.0.1;Port=1;Database=cafemenu_unavailable;Username=test;Password=secret;Timeout=1"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ApiProductionStartup_ShouldFailFastWithRelativeImageStorageRoot()
    {
        using var factory = new ApiProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["ImageStorage:LocalRoot"] = Path.Combine("relative", "media")
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "ImageStorage:LocalRoot must be an absolute filesystem path outside Development.");
    }

    [Fact]
    public async Task WebProductionStartup_ShouldSucceedWithValidDeterministicConfiguration()
    {
        await using var factory = new WebProductionConfigurationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://web.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("AdminApi:BaseUrl", "http://api.example.com", "AdminApi:BaseUrl must use HTTPS outside Development.")]
    [InlineData("AdminApi:BaseUrl", "", "AdminApi:BaseUrl is required.")]
    [InlineData("PublicApi:BaseUrl", "http://api.example.com", "PublicApi:BaseUrl must use HTTPS outside Development.")]
    [InlineData("PublicApi:BaseUrl", "", "PublicApi:BaseUrl is required.")]
    public void WebProductionStartup_ShouldFailFastWithUnsafeApiBaseUrl(
        string configurationKey,
        string configurationValue,
        string expectedMessage)
    {
        using var factory = new WebProductionConfigurationFactory(new Dictionary<string, string?>
        {
            [configurationKey] = configurationValue
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            expectedMessage);
    }

    [Fact]
    public void WebProductionStartup_ShouldFailFastWithMemoryAdminSessionProvider()
    {
        using var factory = new WebProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["AdminSession:Provider"] = AdminSessionProvider.Memory
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            AdminAuthServiceCollectionExtensions.MemoryStoreProductionGuardMessage);
    }

    [Fact]
    public void WebProductionStartup_ShouldFailFastWithRedisProviderAndMissingConnectionString()
    {
        using var factory = new WebProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["AdminSession:RedisConnectionString"] = string.Empty
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "AdminSession:RedisConnectionString is required");
    }

    [Fact]
    public void WebProductionStartup_ShouldFailFastWithMissingDataProtectionKeyRingPath()
    {
        using var factory = new WebProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["DataProtection:KeyRingPath"] = string.Empty
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "DataProtection:KeyRingPath is required outside Development.");
    }

    [Fact]
    public void WebProductionStartup_ShouldFailFastWithWildcardAllowedHosts()
    {
        using var factory = new WebProductionConfigurationFactory(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*"
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "AllowedHosts must not use unrestricted wildcard");
    }

    [Fact]
    public async Task DevelopmentStartup_ShouldKeepRepresentativeLocalConfigurationWorking()
    {
        await using var apiFactory = new ApiProductionConfigurationFactory(
            environmentName: "Development",
            configurationOverrides: new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "localhost;127.0.0.1;[::1];api",
                ["Jwt:SigningKey"] = "development_placeholder_signing_key_change_me_32_chars_min",
                ["ImageStorage:PublicBaseUrl"] = "http://localhost/media",
                ["ImageStorage:LocalRoot"] = string.Empty
            });
        using var apiClient = apiFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        using var apiResponse = await apiClient.GetAsync("/health/live");

        await using var webFactory = new WebProductionConfigurationFactory(
            environmentName: "Development",
            configurationOverrides: new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "localhost;127.0.0.1;[::1];web",
                ["AdminApi:BaseUrl"] = "http://localhost:5279",
                ["PublicApi:BaseUrl"] = "http://localhost:5279",
                ["PublicMenu:BaseUrl"] = "http://localhost:5161",
                ["AdminSession:Provider"] = AdminSessionProvider.Memory,
                ["AdminSession:RedisConnectionString"] = string.Empty,
                ["DataProtection:KeyRingPath"] = string.Empty
            });
        using var webClient = webFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        using var webResponse = await webClient.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, apiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, webResponse.StatusCode);
    }

    private static Exception AssertStartupValidationContains(Action action, string expectedMessage)
    {
        var exception = Record.Exception(action);

        Assert.NotNull(exception);
        Assert.Contains(expectedMessage, exception.ToString(), StringComparison.Ordinal);
        return exception;
    }

    private sealed class ApiProductionConfigurationFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly string _databaseName = $"cafemenu_final_config_{Guid.NewGuid():N}";
        private readonly string _environmentName;
        private readonly Dictionary<string, string?> _previousEnvironmentValues = new(StringComparer.Ordinal);

        public ApiProductionConfigurationFactory(
            IReadOnlyDictionary<string, string?>? configurationOverrides = null,
            string environmentName = "Production")
        {
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
            _environmentName = environmentName;
            ApplyEnvironmentConfiguration(BuildConfiguration());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(BuildConfiguration());
            });
            builder.ConfigureTestServices(services =>
            {
                RemoveDbContextRegistrations(services);
                services.AddDbContext<CafeMenuDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            RestoreEnvironmentConfiguration();
            base.Dispose(disposing);
        }

        private IReadOnlyDictionary<string, string?> BuildConfiguration()
        {
            var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=cafemenu_unavailable;Username=test;Password=secret;Timeout=1",
                ["AllowedHosts"] = "api.example.com",
                ["Jwt:Issuer"] = "CafeMenu.Tests",
                ["Jwt:Audience"] = "CafeMenu.Api",
                ["Jwt:SigningKey"] = "final_config_tests_signing_key_32_chars_min",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
                ["UserSetup:TokenExpirationHours"] = "24",
                ["Database:Retry:Enabled"] = "true",
                ["Database:Retry:MaxRetryCount"] = "3",
                ["Database:Retry:MaxRetryDelaySeconds"] = "5",
                ["ImageStorage:Provider"] = "Local",
                ["ImageStorage:LocalRoot"] = Path.Combine(
                    Path.GetTempPath(),
                    "cafemenu-final-config-media",
                    _databaseName),
                ["ImageStorage:PublicBaseUrl"] = "https://api.example.com/media",
                ["ImageStorage:MaxFileSizeBytes"] = "5242880",
                ["ReverseProxy:Enabled"] = "false",
                ["ReverseProxy:ForwardLimit"] = "1",
                ["RateLimiting:Login:PermitLimit"] = "10",
                ["RateLimiting:Login:WindowSeconds"] = "60",
                ["RateLimiting:Refresh:PermitLimit"] = "60",
                ["RateLimiting:Refresh:WindowSeconds"] = "60",
                ["RateLimiting:AccountSetup:PermitLimit"] = "5",
                ["RateLimiting:AccountSetup:WindowSeconds"] = "300",
                ["RateLimiting:PlatformUserSetup:PermitLimit"] = "20",
                ["RateLimiting:PlatformUserSetup:WindowSeconds"] = "60"
            };

            foreach (var item in _configurationOverrides)
            {
                configuration[item.Key] = item.Value;
            }

            return configuration;
        }

        private void ApplyEnvironmentConfiguration(IReadOnlyDictionary<string, string?> configuration)
        {
            foreach (var item in configuration)
            {
                var environmentKey = item.Key.Replace(":", "__", StringComparison.Ordinal);
                _previousEnvironmentValues[environmentKey] = Environment.GetEnvironmentVariable(environmentKey);
                Environment.SetEnvironmentVariable(environmentKey, item.Value);
            }
        }

        private void RestoreEnvironmentConfiguration()
        {
            foreach (var item in _previousEnvironmentValues)
            {
                Environment.SetEnvironmentVariable(item.Key, item.Value);
            }
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

    private sealed class WebProductionConfigurationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly string _dataProtectionKeyPath = Path.Combine(
            Path.GetTempPath(),
            "cafemenu-final-config-data-protection",
            Guid.NewGuid().ToString("N"));
        private readonly string _environmentName;

        public WebProductionConfigurationFactory(
            IReadOnlyDictionary<string, string?>? configurationOverrides = null,
            string environmentName = "Production")
        {
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
            _environmentName = environmentName;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(BuildConfiguration());
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountSetupApiClient>();
                services.AddSingleton<IAccountSetupApiClient>(new StubAccountSetupApiClient());
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());
                services.RemoveAll<IDistributedCache>();
                services.AddSingleton<IDistributedCache>(new NoopDistributedCache());

                var keyDirectory = new DirectoryInfo(_dataProtectionKeyPath);
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }

        private IReadOnlyDictionary<string, string?> BuildConfiguration()
        {
            var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AllowedHosts"] = "web.example.com",
                ["AdminApi:BaseUrl"] = "https://api.example.com",
                ["PublicApi:BaseUrl"] = "https://api.example.com",
                ["PublicMenu:BaseUrl"] = "https://web.example.com",
                ["HttpClients:DefaultTimeoutSeconds"] = "15",
                ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                ["AdminSession:KeyPrefix"] = "cafemenu:admin-session:",
                ["AdminSession:RedisConnectionString"] = "localhost:6379,abortConnect=false",
                ["AdminSession:MinimumCacheTtlSeconds"] = "1",
                ["DataProtection:ApplicationName"] = "CafeMenu.Web.Tests",
                ["DataProtection:KeyRingPath"] = _dataProtectionKeyPath,
                ["ReverseProxy:Enabled"] = "false",
                ["ReverseProxy:ForwardLimit"] = "1",
                ["RateLimiting:Login:PermitLimit"] = "10",
                ["RateLimiting:Login:WindowSeconds"] = "60",
                ["RateLimiting:Refresh:PermitLimit"] = "60",
                ["RateLimiting:Refresh:WindowSeconds"] = "60",
                ["RateLimiting:AccountSetup:PermitLimit"] = "5",
                ["RateLimiting:AccountSetup:WindowSeconds"] = "300",
                ["RateLimiting:PlatformUserSetup:PermitLimit"] = "20",
                ["RateLimiting:PlatformUserSetup:WindowSeconds"] = "60"
            };

            foreach (var item in _configurationOverrides)
            {
                configuration[item.Key] = item.Value;
            }

            return configuration;
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

    private sealed class NoopDistributedCache : IDistributedCache
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
}
