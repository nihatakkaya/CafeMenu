extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AdminCafeSelectionBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public async Task AdminPage_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login?returnUrl=%2Fadmin", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AdminPage_ShouldCallGetMyCafesForAuthenticatedUser()
    {
        var cafeClient = new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()]));
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            cafeClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, cafeClient.GetMyCafesCallCount);
        Assert.Contains("Mocca Cafe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminPage_ShouldRenderCafeNameStatusAndRoles()
    {
        var cafes = new[]
        {
            CreateCafe(
                id: 10,
                name: "Mocca Cafe",
                slug: "mocca-cafe",
                logoImageUrl: "https://cdn.example.test/logo.png",
                isActive: true,
                isPublished: true,
                roleCodes: [ "CAFE_OWNER" ]),
            CreateCafe(
                id: 20,
                name: "Closed Cafe",
                slug: "closed-cafe",
                isActive: false,
                isPublished: false,
                roleCodes: [ "PLATFORM_ADMIN" ])
        };
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success(cafes)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Mocca Cafe", html, StringComparison.Ordinal);
        Assert.Contains("mocca-cafe", html, StringComparison.Ordinal);
        Assert.Contains("https://cdn.example.test/logo.png", html, StringComparison.Ordinal);
        Assert.Contains("Aktif", html, StringComparison.Ordinal);
        Assert.Contains("Yay&#x131;nda", html, StringComparison.Ordinal);
        Assert.Contains("Closed Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Pasif", html, StringComparison.Ordinal);
        Assert.Contains("Taslak", html, StringComparison.Ordinal);
        Assert.Contains("CAFE_OWNER", html, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_ADMIN", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/10", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminPage_ShouldRenderEmptyCafeState()
    {
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişilebilir cafe yok", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminPage_ShouldRenderSafeApiFailureState()
    {
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cafeler yüklenemedi", html, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCafeShell_ShouldRenderAccessibleCafe()
    {
        var cafe = CreateCafe(id: 42, name: "Shell Cafe", slug: "shell-cafe", roleCodes: [ "CAFE_MANAGER" ]);
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([cafe])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin/cafes/42");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Shell Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Cafe ID", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains(">42<", html, StringComparison.Ordinal);
        Assert.Contains("CAFE_MANAGER", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/42/categories", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/42/products", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/42/branding", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/42/qr", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/42/settings", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Ayarları", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("Dashboard özeti yüklenemedi", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCafeShell_ShouldRenderDashboardStatsForAccessibleCafe()
    {
        var cafe = CreateCafe(id: 42, name: "Stats Cafe", slug: "stats-cafe", isPublished: true);
        var stats = new AdminCafeDashboardStatsResponse
        {
            CafeId = 42,
            CafeName = "Stats Cafe",
            IsActive = true,
            IsPublished = true,
            TotalCategoryCount = 5,
            PublicCategoryCount = 3,
            TotalProductCount = 18,
            PublicProductCount = 12,
            AvailableProductCount = 10,
            UnavailableProductCount = 8
        };
        var cafeClient = new FakeAdminCafeApiClient(
            AdminCafeListResult.Success([cafe]),
            AdminCafeDashboardStatsResult.Success(stats));
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            cafeClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin/cafes/42");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, cafeClient.LastDashboardCafeId);
        Assert.Contains("Toplam kategori", html, StringComparison.Ordinal);
        Assert.Contains(">5<", html, StringComparison.Ordinal);
        Assert.Contains("Yayında / görünür kategori", html, StringComparison.Ordinal);
        Assert.Contains(">3<", html, StringComparison.Ordinal);
        Assert.Contains("Toplam ürün", html, StringComparison.Ordinal);
        Assert.Contains(">18<", html, StringComparison.Ordinal);
        Assert.Contains("Yayında / görünür ürün", html, StringComparison.Ordinal);
        Assert.Contains(">12<", html, StringComparison.Ordinal);
        Assert.Contains("Mevcut ürün", html, StringComparison.Ordinal);
        Assert.Contains(">10<", html, StringComparison.Ordinal);
        Assert.Contains("Tükendi", html, StringComparison.Ordinal);
        Assert.Contains(">8<", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCafeShell_ShouldDenyRouteIdMissingFromGetMyCafes()
    {
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin/cafes/99");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Cafe ID: 99", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCafeSelection_ShouldNotUseBrowserStorage()
    {
        await using var factory = new AdminCafeWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("localStorage", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminCafeApiClient_ShouldUseAuthenticatedAdminHttpClientName()
    {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "success": true,
                  "message": "ok",
                  "data": [
                    {
                      "id": 5,
                      "name": "Named Client Cafe",
                      "slug": "named-client-cafe",
                      "logoImageUrl": null,
                      "isActive": true,
                      "isPublished": false,
                      "roleCodes": [ "CAFE_OWNER" ]
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var factory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminCafeApiClient(factory);

        var result = await apiClient.GetMyCafesAsync(CancellationToken.None);

        Assert.Equal(AdminCafeListStatus.Success, result.Status);
        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, factory.LastClientName);
        Assert.Equal("https://api.example.test/Cafe/GetMyCafes", handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task AdminCafeApiClient_ShouldCallDashboardStatsEndpointWithAuthenticatedClient()
    {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "cafeId": 5,
                    "cafeName": "Named Client Cafe",
                    "isActive": true,
                    "isPublished": false,
                    "totalCategoryCount": 2,
                    "publicCategoryCount": 1,
                    "totalProductCount": 4,
                    "publicProductCount": 3,
                    "availableProductCount": 2,
                    "unavailableProductCount": 2
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var factory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminCafeApiClient(factory);

        var result = await apiClient.GetCafeDashboardStatsAsync(5, CancellationToken.None);

        Assert.Equal(AdminCafeListStatus.Success, result.Status);
        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, factory.LastClientName);
        Assert.Equal("https://api.example.test/Cafe/GetCafeDashboardStats/5", handler.RequestUri?.ToString());
        Assert.Equal(4, result.Stats?.TotalProductCount);
    }

    private static async Task LoginThroughEndpointAsync(HttpClient client)
    {
        using var loginResponse = await client.GetAsync("/account/login?returnUrl=/admin");
        var loginPage = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginPage);

        var antiforgeryToken = ExtractAntiforgeryToken(loginPage);
        using var response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner@example.local",
                ["Password"] = "SecurePassword123!",
                ["ReturnUrl"] = "/admin",
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

    private static AdminCafeResponse CreateCafe(
        long id = 10,
        string name = "Mocca Cafe",
        string slug = "mocca-cafe",
        string? logoImageUrl = null,
        bool isActive = true,
        bool isPublished = false,
        IReadOnlyCollection<string>? roleCodes = null)
    {
        return new AdminCafeResponse
        {
            Id = id,
            Name = name,
            Slug = slug,
            LogoImageUrl = logoImageUrl,
            IsActive = isActive,
            IsPublished = isPublished,
            RoleCodes = roleCodes ?? [ "CAFE_OWNER" ]
        };
    }

    private static AdminAuthResponse CreateAuthResponse()
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
                [ "CAFE_OWNER" ]));
    }

    private sealed class FakeAdminCafeApiClient : IAdminCafeApiClient
    {
        private readonly AdminCafeListResult _result;
        private readonly AdminCafeDashboardStatsResult _dashboardStatsResult;

        public FakeAdminCafeApiClient(
            AdminCafeListResult result,
            AdminCafeDashboardStatsResult? dashboardStatsResult = null)
        {
            _result = result;
            _dashboardStatsResult = dashboardStatsResult ?? AdminCafeDashboardStatsResult.Failure();
        }

        public int GetMyCafesCallCount { get; private set; }

        public long? LastDashboardCafeId { get; private set; }

        public Task<AdminCafeListResult> GetMyCafesAsync(CancellationToken cancellationToken)
        {
            GetMyCafesCallCount++;
            return Task.FromResult(_result);
        }

        public Task<AdminCafeDashboardStatsResult> GetCafeDashboardStatsAsync(
            long cafeId,
            CancellationToken cancellationToken)
        {
            LastDashboardCafeId = cafeId;
            return Task.FromResult(_dashboardStatsResult);
        }
    }

    private sealed class FakeAdminAuthApiClient : IAdminAuthApiClient
    {
        private readonly AdminAuthResponse? _loginResponse;

        public FakeAdminAuthApiClient(AdminAuthResponse? loginResponse = null)
        {
            _loginResponse = loginResponse;
        }

        public Task<AdminAuthResponse?> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResponse);
        }

        public Task<AdminAuthResponse?> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminAuthResponse?>(null);
        }

        public Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class AdminCafeWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;

        public AdminCafeWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthApiClient>();
                services.AddSingleton(_authApiClient);
                services.RemoveAll<IAdminCafeApiClient>();
                services.AddSingleton(_adminCafeApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-cafe-test-data-protection"));
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

    private sealed class RecordingHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public RecordingHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string? LastClientName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            LastClientName = name;
            return _httpClient;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }
}
