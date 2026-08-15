extern alias CafeMenuWeb;

using System.Net;
using CafeMenu.Shared.SecurityHeaders;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class SecurityHeadersTests
{
    [Fact]
    public async Task ApiHealthResponse_ShouldIncludeBaselineSecurityHeaders()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/System/Health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task ApiAuthenticationFailure_ShouldIncludeBaselineSecurityHeaders()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/Authentication/GetCurrentUser");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task ApiMediaResponse_ShouldIncludeBaselineSecurityHeaders()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/media/products/missing.png");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task DevelopmentSwagger_ShouldStillRenderWithBaselineSecurityHeaders()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/index.html");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Swagger UI", html, StringComparison.OrdinalIgnoreCase);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task WebLoginResponse_ShouldIncludeBaselineSecurityHeaders()
    {
        await using var factory = new SecurityHeadersWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task WebPublicMenuResponse_ShouldStillRenderWithBaselineSecurityHeaders()
    {
        await using var factory = new SecurityHeadersWebApplicationFactory(
            PublicMenuRequestResult.Success(CreatePublicMenu()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mocca Cafe", html, StringComparison.Ordinal);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task WebStaticAssetResponse_ShouldIncludeBaselineSecurityHeaders()
    {
        await using var factory = new SecurityHeadersWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/app.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public async Task WebAdminRedirectResponse_ShouldIncludeBaselineSecurityHeaders()
    {
        await using var factory = new SecurityHeadersWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertBaselineSecurityHeaders(response);
    }

    [Fact]
    public void SecurityHeaderRegistration_ShouldDisableKestrelServerHeader()
    {
        var services = new ServiceCollection();

        services.AddApplicationSecurityHeaders();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        Assert.False(options.AddServerHeader);
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
        AssertHeader(
            response,
            ApplicationSecurityHeaders.XFrameOptionsHeaderName,
            ApplicationSecurityHeaders.XFrameOptionsValue);
        AssertHeader(
            response,
            ApplicationSecurityHeaders.PermissionsPolicyHeaderName,
            ApplicationSecurityHeaders.PermissionsPolicyValue);
        AssertHeader(
            response,
            ApplicationSecurityHeaders.ContentSecurityPolicyHeaderName,
            ApplicationSecurityHeaders.ContentSecurityPolicyValue);
        Assert.False(response.Headers.Contains("Server"));
    }

    private static void AssertHeader(HttpResponseMessage response, string headerName, string expectedValue)
    {
        Assert.True(response.Headers.TryGetValues(headerName, out var values), $"{headerName} was missing.");
        Assert.Equal(expectedValue, Assert.Single(values));
    }

    private static PublicMenuResponse CreatePublicMenu()
    {
        return new PublicMenuResponse
        {
            CafeName = "Mocca Cafe",
            Slug = "mocca-cafe",
            Theme = new PublicMenuThemeResponse
            {
                PrimaryColor = "#111827",
                SecondaryColor = "#F9FAFB",
                AccentColor = "#D97706",
                BackgroundColor = "#FFFFFF",
                TextColor = "#111111",
                FontPreset = "SYSTEM",
                ThemePreset = "CLASSIC"
            },
            Categories =
            [
                new PublicMenuCategoryResponse
                {
                    Id = 10,
                    Name = "Kahveler",
                    DisplayOrder = 1,
                    Products =
                    [
                        new PublicMenuProductResponse
                        {
                            Id = 20,
                            Name = "Flat White",
                            Price = 150m,
                            IsAvailable = true,
                            DisplayOrder = 1
                        }
                    ]
                }
            ]
        };
    }

    private sealed class SecurityHeadersWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly PublicMenuRequestResult _publicMenuResult;
        private readonly string _dataProtectionKeyPath = Path.Combine(
            Path.GetTempPath(),
            "cafemenu-security-headers-data-protection",
            Guid.NewGuid().ToString("N"));

        public SecurityHeadersWebApplicationFactory()
            : this(PublicMenuRequestResult.NotFound())
        {
        }

        public SecurityHeadersWebApplicationFactory(PublicMenuRequestResult publicMenuResult)
        {
            _publicMenuResult = publicMenuResult;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient(_publicMenuResult));

                var keyDirectory = new DirectoryInfo(_dataProtectionKeyPath);
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }
    }

    private sealed class StubPublicMenuApiClient : IPublicMenuApiClient
    {
        private readonly PublicMenuRequestResult _publicMenuResult;

        public StubPublicMenuApiClient(PublicMenuRequestResult publicMenuResult)
        {
            _publicMenuResult = publicMenuResult;
        }

        public Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(_publicMenuResult);
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
