extern alias CafeMenuWeb;

using System.Net;
using CafeMenu.Api.Data;
using CafeMenu.Shared.HostFiltering;
using CafeMenuWeb::CafeMenu.Web.AccountSetup;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

[Collection(EnvironmentMutatingTestCollection.Name)]
public sealed class AllowedHostsConfigurationTests
{
    [Theory]
    [InlineData("api.example.com")]
    [InlineData("api.example.com", "admin.example.com")]
    [InlineData("api.example.com;admin.example.com")]
    [InlineData("*.example.com")]
    public void ApiProductionAllowedHostsValidator_ShouldAcceptExplicitHosts(params string[] allowedHosts)
    {
        var result = Validate("Production", allowedHosts);

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData("*", "unrestricted wildcard")]
    [InlineData("", "at least one explicit host")]
    [InlineData("   ", "at least one explicit host")]
    [InlineData("https://api.example.com", "without scheme")]
    [InlineData("api.example.com:443", "must not include a port")]
    [InlineData("api.example.com/path", "without scheme, path")]
    public void ApiProductionAllowedHostsValidator_ShouldRejectUnsafeHosts(
        string allowedHost,
        string expectedFailure)
    {
        var result = Validate("Production", allowedHost);

        AssertValidationFailed(result, expectedFailure);
    }

    [Theory]
    [InlineData("web.example.com")]
    [InlineData("web.example.com", "admin.example.com")]
    [InlineData("web.example.com;admin.example.com")]
    [InlineData("*.example.com")]
    public void WebProductionAllowedHostsValidator_ShouldAcceptExplicitHosts(params string[] allowedHosts)
    {
        var result = Validate("Production", allowedHosts);

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData("*", "unrestricted wildcard")]
    [InlineData("", "at least one explicit host")]
    [InlineData("https://web.example.com", "without scheme")]
    [InlineData("web.example.com:443", "must not include a port")]
    public void WebProductionAllowedHostsValidator_ShouldRejectUnsafeHosts(
        string allowedHost,
        string expectedFailure)
    {
        var result = Validate("Production", allowedHost);

        AssertValidationFailed(result, expectedFailure);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("localhost")]
    [InlineData("localhost", "127.0.0.1", "[::1]", "api")]
    public void DevelopmentAllowedHostsValidator_ShouldAllowLocalDevelopmentHosts(params string[] allowedHosts)
    {
        var result = Validate("Development", allowedHosts);

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void HostFilteringOptions_ShouldRejectEmptyHostsAndHideFailureMessage()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Development"));
        services.AddApplicationHostFiltering();

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var options = serviceProvider.GetRequiredService<IOptions<HostFilteringOptions>>().Value;

        Assert.False(options.AllowEmptyHosts);
        Assert.False(options.IncludeFailureMessage);
    }

    [Fact]
    public void ApiStartupValidation_ShouldFailFastForProductionWildcardAllowedHosts()
    {
        using var factory = new ApiAllowedHostsFactory("Production", allowedHosts: "*");

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("AllowedHosts must not use unrestricted wildcard", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WebStartupValidation_ShouldFailFastForProductionWildcardAllowedHosts()
    {
        using var factory = new WebAllowedHostsFactory("Production", allowedHosts: "*");

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("AllowedHosts must not use unrestricted wildcard", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApiHostFiltering_ShouldAllowConfiguredHost()
    {
        await using var factory = new ApiAllowedHostsFactory("Production", allowedHosts: "api.example.com");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/System/Health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiHostFiltering_ShouldRejectDisallowedHost()
    {
        await using var factory = new ApiAllowedHostsFactory("Production", allowedHosts: "api.example.com");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://evil.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/System/Health");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiHealthEndpoint_ShouldRespectHostFiltering()
    {
        await using var factory = new ApiAllowedHostsFactory("Production", allowedHosts: "api.example.com");
        using var allowedClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
            AllowAutoRedirect = false
        });
        using var disallowedClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://evil.example.com"),
            AllowAutoRedirect = false
        });

        using var allowedResponse = await allowedClient.GetAsync("/health/live");
        using var disallowedResponse = await disallowedClient.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, disallowedResponse.StatusCode);
    }

    [Fact]
    public async Task WebHostFiltering_ShouldAllowConfiguredHost()
    {
        await using var factory = new WebAllowedHostsFactory("Production", allowedHosts: "web.example.com");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://web.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WebHostFiltering_ShouldRejectDisallowedHost()
    {
        await using var factory = new WebAllowedHostsFactory("Production", allowedHosts: "web.example.com");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://evil.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static ValidateOptionsResult Validate(string environmentName, params string[] allowedHosts)
    {
        var options = new HostFilteringOptions();
        foreach (var allowedHost in allowedHosts)
        {
            options.AllowedHosts.Add(allowedHost);
        }

        var validator = new AllowedHostsOptionsValidator(new FakeHostEnvironment(environmentName));
        return validator.Validate(null, options);
    }

    private static void AssertValidationSucceeded(ValidateOptionsResult result)
    {
        Assert.False(result.Failed, result.FailureMessage);
    }

    private static void AssertValidationFailed(ValidateOptionsResult result, string expectedFailure)
    {
        Assert.True(result.Failed);
        Assert.Contains(expectedFailure, string.Join(" ", result.Failures), StringComparison.Ordinal);
    }

    private sealed class ApiAllowedHostsFactory : WebApplicationFactory<Program>
    {
        private readonly string _allowedHosts;
        private readonly string _databaseName = $"cafemenu_allowed_hosts_{Guid.NewGuid():N}";
        private readonly string _environmentName;
        private readonly Dictionary<string, string?> _previousEnvironmentValues = new(StringComparer.Ordinal);

        public ApiAllowedHostsFactory(string environmentName, string allowedHosts)
        {
            _environmentName = environmentName;
            _allowedHosts = allowedHosts;
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
            return new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AllowedHosts"] = _allowedHosts,
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=cafemenu_test;Username=test;Password=test",
                ["Jwt:Issuer"] = "CafeMenu.Tests",
                ["Jwt:Audience"] = "CafeMenu.Api",
                ["Jwt:SigningKey"] = "allowed_hosts_tests_signing_key_32_chars_min",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
                ["UserSetup:TokenExpirationHours"] = "24",
                ["ImageStorage:Provider"] = "Local",
                ["ImageStorage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "cafemenu-tests-media", _databaseName),
                ["ImageStorage:PublicBaseUrl"] = "https://api.example.com/media",
                ["ImageStorage:MaxFileSizeBytes"] = "5242880",
                ["ReverseProxy:Enabled"] = "false",
                ["ReverseProxy:ForwardLimit"] = "1"
            };
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

    private sealed class WebAllowedHostsFactory : WebApplicationFactory<WebProgram>
    {
        private readonly string _allowedHosts;
        private readonly string _dataProtectionKeyPath = Path.Combine(
            Path.GetTempPath(),
            "cafemenu-allowed-hosts-data-protection",
            Guid.NewGuid().ToString("N"));
        private readonly string _environmentName;

        public WebAllowedHostsFactory(string environmentName, string allowedHosts)
        {
            _environmentName = environmentName;
            _allowedHosts = allowedHosts;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AllowedHosts"] = _allowedHosts,
                    ["AdminApi:BaseUrl"] = "https://api.example.com",
                    ["PublicApi:BaseUrl"] = "https://api.example.com",
                    ["PublicMenu:BaseUrl"] = "https://web.example.com",
                    ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                    ["AdminSession:RedisConnectionString"] = "localhost:6379",
                    ["AdminSession:MinimumCacheTtlSeconds"] = "1",
                    ["DataProtection:ApplicationName"] = "CafeMenu.Web.Tests",
                    ["DataProtection:KeyRingPath"] = _dataProtectionKeyPath
                });
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
    }

    private sealed class FakeHostEnvironment : IWebHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ContentRootPath = AppContext.BaseDirectory;
            WebRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new NullFileProvider();
            WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "CafeMenu.Tests";

        public IFileProvider ContentRootFileProvider { get; set; }

        public string ContentRootPath { get; set; }

        public string EnvironmentName { get; set; }

        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }
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
