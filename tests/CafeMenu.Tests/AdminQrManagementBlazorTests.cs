extern alias CafeMenuWeb;

using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminQr;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AdminQrManagementBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";
    private const string PublicMenuBaseUrl = "https://menus.example.test/";

    [Fact]
    public async Task QrPage_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin/cafes/10/qr");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login?returnUrl=%2Fadmin%2Fcafes%2F10%2Fqr", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("CAFE_OWNER")]
    [InlineData("CAFE_MANAGER")]
    [InlineData("PLATFORM_ADMIN")]
    public async Task QrPage_ShouldRenderAccessibleCafeForSupportedAdminRoles(string roleCode)
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ roleCode ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([
                CreateCafe(id: 10, roleCodes: [ roleCode ])
            ])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mocca Cafe", html, StringComparison.Ordinal);
        Assert.Contains("https://menus.example.test/c/mocca-cafe", html, StringComparison.Ordinal);
        Assert.Contains("data:image/png;base64,", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/10/qr/download/png\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/10/qr/download/svg\"", html, StringComparison.Ordinal);
        Assert.Contains("QR Kod", html, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QrPage_ShouldUseSlugFromGetMyCafesAndIgnoreArbitraryUrlQuery()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr?url=https://evil.example.test/c/other");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://menus.example.test/c/mocca-cafe", html, StringComparison.Ordinal);
        Assert.DoesNotContain("evil.example.test", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://menus.example.test//c/mocca-cafe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QrPage_ShouldDenyRouteIdMissingFromGetMyCafesWithoutGeneratingQr()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/qr");

        using var response = await client.GetAsync("/admin/cafes/99/qr");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data:image/png;base64,", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QrPage_ShouldShowWarningForDraftOrInactiveCafe()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([
                CreateCafe(isActive: false, isPublished: false, roleCodes: [ "PLATFORM_ADMIN" ])
            ])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Public menü şu anda yayınlanmayabilir", html, StringComparison.Ordinal);
        Assert.Contains("Pasif", html, StringComparison.Ordinal);
        Assert.Contains("Taslak", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PngDownload_ShouldReturnAttachmentWithPngSignature()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr/download/png");
        var content = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("mocca-cafe-menu-qr.png", GetContentDispositionFileName(response.Content.Headers.ContentDisposition), StringComparison.Ordinal);
        Assert.True(content.Length > 8);
        Assert.Equal([ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A ], content.Take(8).ToArray());
    }

    [Fact]
    public async Task SvgDownload_ShouldReturnAttachmentWithSvgContent()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_MANAGER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr/download/svg");
        var svg = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("mocca-cafe-menu-qr.svg", GetContentDispositionFileName(response.Content.Headers.ContentDisposition), StringComparison.Ordinal);
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Download_ShouldReturnNotFoundForInaccessibleCafe()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/qr");

        using var response = await client.GetAsync("/admin/cafes/99/qr/download/png");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_ShouldUseSafeFileNameFromBackendSlug()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([
                CreateCafe(slug: "Mocca Cafe../Bad", roleCodes: [ "PLATFORM_ADMIN" ])
            ])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr/download/png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("mocca-cafe-bad-menu-qr.png", GetContentDispositionFileName(response.Content.Headers.ContentDisposition), StringComparison.Ordinal);
        Assert.DoesNotContain("..", GetContentDispositionFileName(response.Content.Headers.ContentDisposition), StringComparison.Ordinal);
        Assert.DoesNotContain("/", GetContentDispositionFileName(response.Content.Headers.ContentDisposition), StringComparison.Ordinal);
    }

    [Fact]
    public async Task QrService_ShouldEncodeDeterministicPublicMenuUrl()
    {
        var renderer = new RecordingQrCodeRenderer();
        var service = new AdminQrCodeService(
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])),
            new FixedAdminQrUrlBuilder("https://menus.example.test/c/mocca-cafe"),
            renderer);

        var result = await service.GetDownloadAsync(10, AdminQrDownloadFormat.Png, CancellationToken.None);

        Assert.Equal(AdminQrRequestStatus.Success, result.Status);
        Assert.Equal("https://menus.example.test/c/mocca-cafe", renderer.LastPngContent);
        Assert.Equal("https://menus.example.test/c/mocca-cafe", result.File?.EncodedUrl);
    }

    [Fact]
    public void QrUrlBuilder_ShouldNormalizeTrailingSlashAndEscapeSlug()
    {
        var builder = new AdminQrUrlBuilder(Options.Create(new PublicMenuQrOptions
        {
            BaseUrl = "https://menus.example.test/"
        }));

        var url = builder.BuildPublicMenuUrl("mocca cafe");

        Assert.Equal("https://menus.example.test/c/mocca%20cafe", url);
    }

    [Fact]
    public void ProductionOptionsValidator_ShouldRejectLocalhostPublicMenuBaseUrl()
    {
        var validator = new AdminQrOptionsValidator(new FakeWebHostEnvironment("Production"));

        var result = validator.Validate(
            Options.DefaultName,
            new PublicMenuQrOptions { BaseUrl = "http://localhost:5161" });

        Assert.True(result.Failed);
        Assert.Contains("must not point to localhost outside Development", string.Join(" ", result.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public async Task QrPage_ShouldNotUseBrowserStorageOrRawMarkup()
    {
        await using var factory = new AdminQrWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/qr");

        using var response = await client.GetAsync("/admin/cafes/10/qr");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("localStorage", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task LoginThroughEndpointAsync(HttpClient client, string returnUrl)
    {
        using var loginResponse = await client.GetAsync($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var loginPage = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginPage);

        var antiforgeryToken = ExtractAntiforgeryToken(loginPage);
        using var response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner@example.local",
                ["Password"] = "SecurePassword123!",
                ["ReturnUrl"] = returnUrl,
                ["__RequestVerificationToken"] = antiforgeryToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, html);
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static string GetContentDispositionFileName(ContentDispositionHeaderValue? value)
    {
        return value?.FileNameStar ?? value?.FileName?.Trim('"') ?? string.Empty;
    }

    private static AdminCafeResponse CreateCafe(
        long id = 10,
        string name = "Mocca Cafe",
        string slug = "mocca-cafe",
        bool isActive = true,
        bool isPublished = true,
        IReadOnlyCollection<string>? roleCodes = null)
    {
        return new AdminCafeResponse
        {
            Id = id,
            Name = name,
            Slug = slug,
            LogoImageUrl = "https://cdn.example.test/logo.png",
            IsActive = isActive,
            IsPublished = isPublished,
            RoleCodes = roleCodes ?? [ "CAFE_OWNER" ]
        };
    }

    private static AdminAuthResponse CreateAuthResponse(IReadOnlyCollection<string> roleCodes)
    {
        return new AdminAuthResponse(
            AccessToken,
            RefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow.AddDays(7),
            new AdminUserResponse(
                10,
                "owner@example.local",
                "Cafe Owner",
                roleCodes));
    }

    private sealed class FakeAdminCafeApiClient : IAdminCafeApiClient
    {
        private readonly AdminCafeListResult _result;

        public FakeAdminCafeApiClient(AdminCafeListResult result)
        {
            _result = result;
        }

        public Task<AdminCafeListResult> GetMyCafesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeAdminAuthApiClient : IAdminAuthApiClient
    {
        private readonly AdminAuthResponse? _loginResponse;

        public FakeAdminAuthApiClient(AdminAuthResponse? loginResponse = null)
        {
            _loginResponse = loginResponse;
        }

        public Task<AdminAuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResponse);
        }

        public Task<AdminAuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminAuthResponse?>(null);
        }

        public Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingQrCodeRenderer : IAdminQrCodeRenderer
    {
        public string? LastPngContent { get; private set; }

        public byte[] GeneratePng(string content)
        {
            LastPngContent = content;
            return [ 1, 2, 3 ];
        }

        public string GenerateSvg(string content)
        {
            return "<svg></svg>";
        }

        public byte[] GenerateSvgBytes(string content)
        {
            return [ 4, 5, 6 ];
        }
    }

    private sealed class FixedAdminQrUrlBuilder : IAdminQrUrlBuilder
    {
        private readonly string _url;

        public FixedAdminQrUrlBuilder(string url)
        {
            _url = url;
        }

        public string BuildPublicMenuUrl(string slug)
        {
            return _url;
        }
    }

    private sealed class AdminQrWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;

        public AdminQrWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PublicMenu:BaseUrl"] = PublicMenuBaseUrl
                });
            });
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthApiClient>();
                services.AddSingleton(_authApiClient);
                services.RemoveAll<IAdminCafeApiClient>();
                services.AddSingleton(_adminCafeApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-qr-test-data-protection"));
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
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

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "CafeMenu.Web";

        public IFileProvider WebRootFileProvider { get; set; } = null!;

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; }

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
