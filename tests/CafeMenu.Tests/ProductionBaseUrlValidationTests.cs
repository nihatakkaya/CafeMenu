extern alias CafeMenuWeb;

using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class ProductionBaseUrlValidationTests
{
    [Theory]
    [InlineData("http://localhost:5279")]
    [InlineData("http://127.0.0.1:5279")]
    [InlineData("http://[::1]:5279")]
    public void AdminApiValidator_ShouldAllowDevelopmentLocalHttp(string baseUrl)
    {
        var result = ValidateAdminApi("Development", baseUrl);

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData("http://localhost:5279", "localhost or loopback")]
    [InlineData("https://localhost:5279", "localhost or loopback")]
    [InlineData("http://127.0.0.1:5279", "localhost or loopback")]
    [InlineData("https://127.0.0.1:5279", "localhost or loopback")]
    [InlineData("http://api.example.com", "HTTPS outside Development")]
    [InlineData("api.example.com", "absolute URI")]
    [InlineData("ftp://api.example.com", "HTTP or HTTPS")]
    public void AdminApiValidator_ShouldRejectUnsafeOrMalformedProductionUrls(
        string baseUrl,
        string expectedFailure)
    {
        var result = ValidateAdminApi("Production", baseUrl);

        AssertValidationFailed(result, expectedFailure);
    }

    [Fact]
    public void AdminApiValidator_ShouldAllowProductionHttpsRemoteHost()
    {
        var result = ValidateAdminApi("Production", "https://api.example.com");

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData("http://localhost:5279")]
    [InlineData("http://127.0.0.1:5279")]
    [InlineData("http://[::1]:5279")]
    public void PublicApiValidator_ShouldAllowDevelopmentLocalHttp(string baseUrl)
    {
        var result = ValidatePublicApi("Development", baseUrl);

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData("http://localhost:5279", "localhost or loopback")]
    [InlineData("https://localhost:5279", "localhost or loopback")]
    [InlineData("http://127.0.0.1:5279", "localhost or loopback")]
    [InlineData("https://127.0.0.1:5279", "localhost or loopback")]
    [InlineData("http://api.example.com", "HTTPS outside Development")]
    [InlineData("api.example.com", "absolute URI")]
    [InlineData("ftp://api.example.com", "HTTP or HTTPS")]
    public void PublicApiValidator_ShouldRejectUnsafeOrMalformedProductionUrls(
        string baseUrl,
        string expectedFailure)
    {
        var result = ValidatePublicApi("Production", baseUrl);

        AssertValidationFailed(result, expectedFailure);
    }

    [Fact]
    public void PublicApiValidator_ShouldAllowProductionHttpsRemoteHost()
    {
        var result = ValidatePublicApi("Production", "https://api.example.com");

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void StartupValidation_ShouldFailFastForInvalidProductionAdminApiBaseUrl()
    {
        using var factory = new ProductionBaseUrlWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AdminApi:BaseUrl"] = "http://api.example.com"
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "AdminApi:BaseUrl must use HTTPS outside Development.");
    }

    [Fact]
    public void StartupValidation_ShouldFailFastForInvalidProductionPublicApiBaseUrl()
    {
        using var factory = new ProductionBaseUrlWebApplicationFactory(new Dictionary<string, string?>
        {
            ["PublicApi:BaseUrl"] = "http://api.example.com"
        });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "PublicApi:BaseUrl must use HTTPS outside Development.");
    }

    [Fact]
    public async Task StartupValidation_ShouldAllowProductionHttpsRemoteBaseUrls()
    {
        using var factory = new ProductionBaseUrlWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://web.example.com"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/account/login");

        Assert.True(response.IsSuccessStatusCode);
    }

    private static ValidateOptionsResult ValidateAdminApi(string environmentName, string baseUrl)
    {
        var validator = new AdminApiOptionsValidator(new FakeWebHostEnvironment(environmentName));
        return validator.Validate(null, new AdminApiOptions { BaseUrl = baseUrl });
    }

    private static ValidateOptionsResult ValidatePublicApi(string environmentName, string baseUrl)
    {
        var validator = new PublicApiOptionsValidator(new FakeWebHostEnvironment(environmentName));
        return validator.Validate(null, new PublicMenuApiOptions { BaseUrl = baseUrl });
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

    private static Exception AssertStartupValidationContains(Action action, string expectedMessage)
    {
        var exception = Record.Exception(action);

        Assert.NotNull(exception);
        Assert.Contains(expectedMessage, exception.ToString(), StringComparison.Ordinal);
        return exception;
    }

    private sealed class ProductionBaseUrlWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly string _dataProtectionKeyPath = Path.Combine(
            Path.GetTempPath(),
            "cafemenu-production-base-url-data-protection",
            Guid.NewGuid().ToString("N"));

        public ProductionBaseUrlWebApplicationFactory(
            IReadOnlyDictionary<string, string?>? configurationOverrides = null)
        {
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(BuildConfiguration());
            });
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                var keyDirectory = new DirectoryInfo(_dataProtectionKeyPath);
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }

        private IReadOnlyDictionary<string, string?> BuildConfiguration()
        {
            var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AdminApi:BaseUrl"] = "https://api.example.com",
                ["PublicApi:BaseUrl"] = "https://api.example.com",
                ["PublicMenu:BaseUrl"] = "https://menu.example.com",
                ["AllowedHosts"] = "web.example.com",
                ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                ["AdminSession:RedisConnectionString"] = "localhost:6379",
                ["AdminSession:MinimumCacheTtlSeconds"] = "1",
                ["DataProtection:ApplicationName"] = "CafeMenu.Web.Tests",
                ["DataProtection:KeyRingPath"] = _dataProtectionKeyPath
            };

            foreach (var item in _configurationOverrides)
            {
                configuration[item.Key] = item.Value;
            }

            return configuration;
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string environmentName)
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
}
