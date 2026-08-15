extern alias CafeMenuWeb;

using System.Security.Cryptography;
using CafeMenuWeb::CafeMenu.Web.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace CafeMenu.Tests;

public sealed class WebDataProtectionConfigurationTests
{
    [Fact]
    public void Validator_ShouldAllowDevelopmentWithDefaultApplicationNameAndEmptyKeyRingPath()
    {
        var result = Validate("Development", new WebDataProtectionOptions());

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void Validator_ShouldRejectProductionWithEmptyKeyRingPath()
    {
        var result = Validate("Production", new WebDataProtectionOptions
        {
            ApplicationName = "CafeMenu.Web",
            KeyRingPath = string.Empty
        });

        AssertValidationFailed(result, "DataProtection:KeyRingPath is required outside Development.");
    }

    [Fact]
    public void Validator_ShouldRejectProductionWithWhitespaceKeyRingPath()
    {
        var result = Validate("Production", new WebDataProtectionOptions
        {
            ApplicationName = "CafeMenu.Web",
            KeyRingPath = "   "
        });

        AssertValidationFailed(result, "DataProtection:KeyRingPath is required outside Development.");
    }

    [Fact]
    public void Validator_ShouldRejectProductionWithRelativeKeyRingPath()
    {
        var result = Validate("Production", new WebDataProtectionOptions
        {
            ApplicationName = "CafeMenu.Web",
            KeyRingPath = Path.Combine("relative", "keys")
        });

        AssertValidationFailed(result, "valid absolute filesystem path");
    }

    [Fact]
    public void Validator_ShouldAllowProductionWithAbsoluteKeyRingPath()
    {
        var result = Validate("Production", new WebDataProtectionOptions
        {
            ApplicationName = "CafeMenu.Web",
            KeyRingPath = CreateAbsoluteTestPath()
        });

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_ShouldRejectEmptyApplicationName(string applicationName)
    {
        var result = Validate("Development", new WebDataProtectionOptions
        {
            ApplicationName = applicationName,
            KeyRingPath = string.Empty
        });

        AssertValidationFailed(result, "DataProtection:ApplicationName is required.");
    }

    [Fact]
    public void Registration_ShouldExposeConfiguredOptions()
    {
        using var directory = TempDirectory.Create();
        using var serviceProvider = BuildServiceProvider(
            "CafeMenu.Web.Tests",
            directory.Path,
            "Production");

        var options = serviceProvider.GetRequiredService<IOptions<WebDataProtectionOptions>>().Value;

        Assert.Equal("CafeMenu.Web.Tests", options.ApplicationName);
        Assert.Equal(directory.Path, options.KeyRingPath);
    }

    [Fact]
    public void Registration_ShouldPersistKeysToConfiguredFileSystemPath()
    {
        using var directory = TempDirectory.Create();
        using var serviceProvider = BuildServiceProvider(
            "CafeMenu.Web.Tests",
            directory.Path,
            "Production");

        var protector = serviceProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("cafemenu-test-purpose");

        _ = protector.Protect("payload");

        Assert.Contains(
            Directory.EnumerateFiles(directory.Path, "*.xml", SearchOption.TopDirectoryOnly),
            file => Path.GetFileName(file).StartsWith("key-", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Persistence_ShouldAllowSecondProviderWithSamePathAndApplicationNameToUnprotect()
    {
        using var directory = TempDirectory.Create();
        string protectedPayload;

        using (var firstProvider = BuildServiceProvider("CafeMenu.Web.Tests", directory.Path, "Production"))
        {
            protectedPayload = firstProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("admin-cookie-test")
                .Protect("admin-session-payload");
        }

        using var secondProvider = BuildServiceProvider("CafeMenu.Web.Tests", directory.Path, "Production");
        var unprotectedPayload = secondProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("admin-cookie-test")
            .Unprotect(protectedPayload);

        Assert.Equal("admin-session-payload", unprotectedPayload);
    }

    [Fact]
    public void Isolation_ShouldRejectPayloadProtectedWithDifferentApplicationName()
    {
        using var directory = TempDirectory.Create();
        string protectedPayload;

        using (var firstProvider = BuildServiceProvider("CafeMenu.Web.Tests.A", directory.Path, "Production"))
        {
            protectedPayload = firstProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("admin-cookie-test")
                .Protect("admin-session-payload");
        }

        using var secondProvider = BuildServiceProvider("CafeMenu.Web.Tests.B", directory.Path, "Production");
        var exception = Record.Exception(() => secondProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("admin-cookie-test")
            .Unprotect(protectedPayload));

        Assert.IsType<CryptographicException>(exception);
    }

    private static ValidateOptionsResult Validate(string environmentName, WebDataProtectionOptions options)
    {
        var validator = new WebDataProtectionOptionsValidator(new FakeWebHostEnvironment(environmentName));
        return validator.Validate(null, options);
    }

    private static ServiceProvider BuildServiceProvider(
        string applicationName,
        string keyRingPath,
        string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:ApplicationName"] = applicationName,
                ["DataProtection:KeyRingPath"] = keyRingPath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment(environmentName));
        services.AddApplicationDataProtection(configuration);

        return services.BuildServiceProvider();
    }

    private static string CreateAbsoluteTestPath()
    {
        return Path.Combine(Path.GetTempPath(), "cafemenu-data-protection-test", Guid.NewGuid().ToString("N"));
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

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            return new TempDirectory(CreateAbsoluteTestPath());
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
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
