using System.Net;
using CafeMenu.Shared.ReverseProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CafeMenu.Tests;

[Collection(EnvironmentMutatingTestCollection.Name)]
public sealed class ReverseProxyConfigurationTests
{
    [Fact]
    public void Validator_ShouldAllowDisabledConfigurationWithoutTrustedProxyList()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = false,
            ForwardLimit = 1,
            KnownProxies = [],
            KnownIPNetworks = []
        });

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void Validator_ShouldRejectEnabledConfigurationWithoutTrustedProxyList()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = [],
            KnownIPNetworks = []
        });

        AssertValidationFailed(result, "at least one trusted KnownProxy or KnownIPNetwork");
    }

    [Fact]
    public void Validator_ShouldAllowEnabledConfigurationWithValidKnownProxy()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = ["127.0.0.1"],
            KnownIPNetworks = []
        });

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void Validator_ShouldAllowEnabledConfigurationWithValidKnownIPNetwork()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = [],
            KnownIPNetworks = ["10.0.0.0/24"]
        });

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void Validator_ShouldRejectMalformedKnownProxy()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = ["not-an-ip"],
            KnownIPNetworks = []
        });

        AssertValidationFailed(result, "invalid IP address");
    }

    [Fact]
    public void Validator_ShouldRejectMalformedKnownIPNetwork()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = [],
            KnownIPNetworks = ["10.0.0.0/not-a-prefix"]
        });

        AssertValidationFailed(result, "invalid CIDR network");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldRejectNonPositiveForwardLimit(int forwardLimit)
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = forwardLimit,
            KnownProxies = ["127.0.0.1"],
            KnownIPNetworks = []
        });

        AssertValidationFailed(result, "ForwardLimit");
    }

    [Fact]
    public void Validator_ShouldAllowPositiveForwardLimit()
    {
        var result = Validate(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 2,
            KnownProxies = ["127.0.0.1"],
            KnownIPNetworks = []
        });

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void ForwardedHeadersOptions_ShouldUseOnlyForwardedForAndProto()
    {
        var options = BuildForwardedHeadersOptions(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 2,
            KnownProxies = ["127.0.0.1"],
            KnownIPNetworks = []
        });

        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
        Assert.False(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
    }

    [Fact]
    public void ForwardedHeadersOptions_ShouldCopyKnownProxyAndForwardLimit()
    {
        var options = BuildForwardedHeadersOptions(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 3,
            KnownProxies = ["127.0.0.1"],
            KnownIPNetworks = []
        });

        var knownProxy = Assert.Single(options.KnownProxies);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), knownProxy);
        Assert.Equal(3, options.ForwardLimit);
    }

    [Fact]
    public void ForwardedHeadersOptions_ShouldCopyKnownIPNetwork()
    {
        var options = BuildForwardedHeadersOptions(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = [],
            KnownIPNetworks = ["10.0.0.0/24"]
        });

        var knownNetwork = Assert.Single(options.KnownIPNetworks);
        Assert.Equal(IPAddress.Parse("10.0.0.0"), knownNetwork.BaseAddress);
        Assert.Equal(24, knownNetwork.PrefixLength);
    }

    [Fact]
    public void ForwardedHeadersOptions_ShouldNotUseTrustAllConfiguration()
    {
        var options = BuildForwardedHeadersOptions(new ReverseProxyOptions
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownProxies = ["127.0.0.1"],
            KnownIPNetworks = []
        });

        Assert.Single(options.KnownProxies);
        Assert.Empty(options.KnownIPNetworks);
    }

    [Fact]
    public void StartupValidation_ShouldFailFastWhenEnabledWithoutTrustedProxyList()
    {
        using var factory = new ApiReverseProxyApplicationFactory(new Dictionary<string, string?>
        {
            ["ReverseProxy:Enabled"] = "true",
            ["ReverseProxy:ForwardLimit"] = "1"
        });

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(
            "ReverseProxy requires at least one trusted KnownProxy or KnownIPNetwork when Enabled is true.",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Api_ShouldApplyHstsOutsideDevelopment()
    {
        using var factory = new ApiReverseProxyApplicationFactory(environmentName: "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/System/Health");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Api_ShouldNotApplyHstsInDevelopment()
    {
        using var factory = new ApiReverseProxyApplicationFactory(environmentName: "Development");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/System/Health");

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task TrustedProxyForwardedProtoHttps_ShouldBeTreatedAsHttps()
    {
        using var factory = new ApiReverseProxyApplicationFactory(
            new Dictionary<string, string?>
            {
                ["ReverseProxy:Enabled"] = "true",
                ["ReverseProxy:KnownIPNetworks:0"] = "::1/128",
                ["ReverseProxy:KnownIPNetworks:1"] = "127.0.0.1/32"
            },
            "Production");

        var context = await factory.Server.SendAsync(httpContext =>
        {
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.Path = "/System/Health";
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("api.example.com");
            httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;
            httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
            httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task UntrustedProxyForwardedProtoHttps_ShouldNotChangeScheme()
    {
        using var factory = new ApiReverseProxyApplicationFactory(
            new Dictionary<string, string?>
            {
                ["ReverseProxy:Enabled"] = "true",
                ["ReverseProxy:KnownProxies:0"] = "203.0.113.1"
            },
            "Production");

        var context = await factory.Server.SendAsync(httpContext =>
        {
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.Path = "/System/Health";
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("api.example.com");
            httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;
            httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
            httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        });

        Assert.Equal(StatusCodes.Status307TemporaryRedirect, context.Response.StatusCode);
    }

    private static ValidateOptionsResult Validate(ReverseProxyOptions options)
    {
        return new ReverseProxyOptionsValidator().Validate(null, options);
    }

    private static ForwardedHeadersOptions BuildForwardedHeadersOptions(ReverseProxyOptions options)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IOptions<ReverseProxyOptions>>(Options.Create(options));
        services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ReverseProxyForwardedHeadersOptionsSetup>();

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
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

    private sealed class ApiReverseProxyApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly string _environmentName;
        private readonly Dictionary<string, string?> _previousEnvironmentValues = new(StringComparer.Ordinal);

        public ApiReverseProxyApplicationFactory(
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
                services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = 443);
            });
        }

        private IReadOnlyDictionary<string, string?> BuildConfiguration()
        {
            var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=cafemenu_test;Username=test;Password=test",
                ["Jwt:Issuer"] = "CafeMenu.Tests",
                ["Jwt:Audience"] = "CafeMenu.Api",
                ["Jwt:SigningKey"] = "reverse_proxy_tests_signing_key_32_chars_min",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
                ["UserSetup:TokenExpirationHours"] = "24",
                ["ImageStorage:Provider"] = "Local",
                ["ImageStorage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "cafemenu-tests-media", Guid.NewGuid().ToString("N")),
                ["ImageStorage:PublicBaseUrl"] = "https://api.example.com/media",
                ["ImageStorage:MaxFileSizeBytes"] = "5242880",
                ["ReverseProxy:Enabled"] = "false",
                ["ReverseProxy:ForwardLimit"] = "1"
            };

            foreach (var item in _configurationOverrides)
            {
                configuration[item.Key] = item.Value;
            }

            return configuration;
        }

        protected override void Dispose(bool disposing)
        {
            RestoreEnvironmentConfiguration();
            base.Dispose(disposing);
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
    }
}
