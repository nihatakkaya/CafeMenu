extern alias CafeMenuWeb;

using System.Net;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminBranding;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminCafeSettings;
using CafeMenuWeb::CafeMenu.Web.AdminCategory;
using CafeMenuWeb::CafeMenu.Web.AdminProduct;
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

public sealed class AdminPanelIntegrationShellTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    public static TheoryData<string, string, string> AdminRoutes =>
        new()
        {
            { "/admin/cafes/42", "/admin/cafes/42", "Genel" },
            { "/admin/cafes/42/settings", "/admin/cafes/42/settings", "Cafe Ayarları" },
            { "/admin/cafes/42/categories", "/admin/cafes/42/categories", "Kategoriler" },
            { "/admin/cafes/42/products", "/admin/cafes/42/products", "Ürünler" },
            { "/admin/cafes/42/branding", "/admin/cafes/42/branding", "Görünüm" },
            { "/admin/cafes/42/qr", "/admin/cafes/42/qr", "QR Kod" }
        };

    [Theory]
    [MemberData(nameof(AdminRoutes))]
    public async Task CafeAdminRoutes_ShouldRenderCommonNavigationWithCorrectCafeContext(
        string route,
        string activeHref,
        string activeLabel)
    {
        await using var factory = new AdminPanelWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, route);

        using var response = await client.GetAsync(route);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Integration Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Yönetim konsolu", html, StringComparison.Ordinal);
        Assert.Contains("Cafe seçimi", html, StringComparison.Ordinal);
        Assert.Contains("integration-cafe", html, StringComparison.Ordinal);
        Assert.Contains("Pasif", html, StringComparison.Ordinal);
        Assert.Contains("Taslak", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Sahibi", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Yöneticisi", html, StringComparison.Ordinal);
        Assert.DoesNotContain("CAFE_OWNER", html, StringComparison.Ordinal);
        Assert.DoesNotContain("CAFE_MANAGER", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/42\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/42/settings\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/42/categories\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/42/products\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/42/branding\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/42/qr\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/c/integration-cafe\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin\"", html, StringComparison.Ordinal);
        AssertActiveNavigation(html, activeHref, activeLabel);
    }

    [Fact]
    public async Task CafeOverview_ShouldRenderUsefulManagementCardsWithoutAnalyticsCalls()
    {
        var categoryClient = new FakeAdminCategoryApiClient();
        var productClient = new FakeAdminProductApiClient();
        await using var factory = new AdminPanelWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])),
            categoryClient,
            productClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/42");

        using var response = await client.GetAsync("/admin/cafes/42");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cafe Ayarları", html, StringComparison.Ordinal);
        Assert.Contains("Kategoriler", html, StringComparison.Ordinal);
        Assert.Contains("Ürünler", html, StringComparison.Ordinal);
        Assert.Contains("Görünüm", html, StringComparison.Ordinal);
        Assert.Contains("QR Kod", html, StringComparison.Ordinal);
        Assert.Contains("Müşteri Menüsü", html, StringComparison.Ordinal);
        Assert.Equal(0, categoryClient.GetCategoriesCallCount);
        Assert.Equal(0, productClient.GetProductsCallCount);
    }

    [Fact]
    public async Task InaccessibleCafe_ShouldKeepSafeDeniedStateWithoutFeatureApiCalls()
    {
        var categoryClient = new FakeAdminCategoryApiClient();
        var productClient = new FakeAdminProductApiClient();
        var brandingClient = new FakeAdminBrandingApiClient();
        var settingsClient = new FakeAdminCafeSettingsApiClient();
        await using var factory = new AdminPanelWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 42)])),
            categoryClient,
            productClient,
            brandingClient,
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/products");

        using var response = await client.GetAsync("/admin/cafes/99/products");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.Equal(0, categoryClient.GetCategoriesCallCount);
        Assert.Equal(0, productClient.GetProductsCallCount);
        Assert.Equal(0, brandingClient.GetBrandingCallCount);
        Assert.Equal(0, settingsClient.GetSettingsCallCount);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminSelection_ShouldRemainProtectedAndUseManageRoute()
    {
        await using var factory = new AdminPanelWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var anonymousResponse = await anonymousClient.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, anonymousResponse.StatusCode);
        Assert.Equal("/account/login?returnUrl=%2Fadmin", anonymousResponse.Headers.Location?.OriginalString);

        await using var authenticatedFactory = new AdminPanelWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(isActive: false, isPublished: false)])));
        using var client = authenticatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin");

        using var response = await client.GetAsync("/admin");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("href=\"/admin/cafes/42\"", html, StringComparison.Ordinal);
        Assert.Contains("Pasif", html, StringComparison.Ordinal);
        Assert.Contains("Taslak", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogoutForm_ShouldRemainPostWithAntiforgeryOnAdminPages()
    {
        await using var factory = new AdminPanelWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/42");

        using var response = await client.GetAsync("/admin/cafes/42");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("method=\"post\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action=\"/account/logout\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/account/logout\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminPanelShell_ShouldNotUseBrowserStorageOrRawMarkup()
    {
        var root = FindRepositoryRoot();
        var source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(root, "src", "CafeMenu.Web", "Components"), "Admin*.razor", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("localStorage", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", source, StringComparison.OrdinalIgnoreCase);
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

    private static void AssertActiveNavigation(string html, string href, string label)
    {
        var pattern = $@"<a(?=[^>]*href=""{Regex.Escape(href)}"")(?=[^>]*aria-current=""page"")(?=[^>]*admin-cafe-nav-link-active)[^>]*>{Regex.Escape(label)}</a>";
        Assert.Matches(
            new Regex(pattern, RegexOptions.CultureInvariant),
            html);
    }

    private static AdminCafeResponse CreateCafe(
        long id = 42,
        string name = "Integration Cafe",
        string slug = "integration-cafe",
        bool isActive = false,
        bool isPublished = false,
        IReadOnlyCollection<string>? roleCodes = null)
    {
        return new AdminCafeResponse
        {
            Id = id,
            Name = name,
            Slug = slug,
            IsActive = isActive,
            IsPublished = isPublished,
            RoleCodes = roleCodes ?? [ "CAFE_MANAGER", "CAFE_OWNER" ]
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "CafeMenu.Web",
                "Components",
                "Admin",
                "AdminCafeHeader.razor");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located.");
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

        public Task<AdminCafeDashboardStatsResult> GetCafeDashboardStatsAsync(
            long cafeId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCafeDashboardStatsResult.Failure());
        }
    }

    private sealed class FakeAdminCategoryApiClient : IAdminCategoryApiClient
    {
        public int GetCategoriesCallCount { get; private set; }

        public Task<AdminCategoryListResult> GetCategoriesAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetCategoriesCallCount++;
            return Task.FromResult(AdminCategoryListResult.Success([
                new AdminCategoryResponse
                {
                    Id = 7,
                    CafeId = cafeId,
                    Name = "Tatlılar",
                    DisplayOrder = 0,
                    IsVisible = true,
                    IsPublished = true
                }
            ]));
        }

        public Task<AdminCategoryMutationResult> CreateCategoryAsync(AdminCreateCategoryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryMutationResult> UpdateCategoryAsync(long categoryId, AdminUpdateCategoryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryDeleteResult> DeleteCategoryAsync(long categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryDeleteResult.Failure());
        }

        public Task<AdminCategoryMutationResult> ChangeCategoryVisibilityAsync(
            long categoryId,
            AdminChangeCategoryVisibilityRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryMutationResult> ChangeCategoryPublicationAsync(
            long categoryId,
            AdminChangeCategoryPublicationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryListResult> ReorderCategoriesAsync(
            AdminReorderCategoriesRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryListResult.Failure());
        }
    }

    private sealed class FakeAdminProductApiClient : IAdminProductApiClient
    {
        public int GetProductsCallCount { get; private set; }

        public Task<AdminProductListResult> GetProductsAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetProductsCallCount++;
            return Task.FromResult(AdminProductListResult.Success([]));
        }

        public Task<AdminProductMutationResult> CreateProductAsync(AdminCreateProductRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductMutationResult.Failure());
        }

        public Task<AdminProductMutationResult> UpdateProductAsync(long productId, AdminUpdateProductRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductMutationResult.Failure());
        }

        public Task<AdminProductDeleteResult> DeleteProductAsync(long productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductDeleteResult.Failure());
        }

        public Task<AdminProductMutationResult> ChangeProductVisibilityAsync(
            long productId,
            AdminChangeProductVisibilityRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductMutationResult.Failure());
        }

        public Task<AdminProductMutationResult> ChangeProductAvailabilityAsync(
            long productId,
            AdminChangeProductAvailabilityRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductMutationResult.Failure());
        }

        public Task<AdminProductMutationResult> ChangeProductPublicationAsync(
            long productId,
            AdminChangeProductPublicationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductMutationResult.Failure());
        }

        public Task<AdminProductListResult> ReorderProductsAsync(
            AdminReorderProductsRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductListResult.Failure());
        }
    }

    private sealed class FakeAdminBrandingApiClient : IAdminBrandingApiClient
    {
        public int GetBrandingCallCount { get; private set; }

        public Task<AdminBrandingRequestResult> GetCafeBrandingAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetBrandingCallCount++;
            return Task.FromResult(AdminBrandingRequestResult.Success(new AdminBrandingResponse
            {
                CafeId = cafeId,
                CafeName = "Integration Cafe",
                PrimaryColor = AdminBrandingConstants.DefaultPrimaryColor,
                SecondaryColor = AdminBrandingConstants.DefaultSecondaryColor,
                AccentColor = AdminBrandingConstants.DefaultAccentColor,
                BackgroundColor = AdminBrandingConstants.DefaultBackgroundColor,
                TextColor = AdminBrandingConstants.DefaultTextColor,
                FontPreset = AdminBrandingConstants.SystemFontPreset,
                ThemePreset = AdminBrandingConstants.ClassicThemePreset,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }));
        }

        public Task<AdminBrandingRequestResult> UpdateCafeBrandingAsync(
            long cafeId,
            AdminUpdateCafeBrandingRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminBrandingRequestResult.Failure());
        }
    }

    private sealed class FakeAdminCafeSettingsApiClient : IAdminCafeSettingsApiClient
    {
        public int GetSettingsCallCount { get; private set; }

        public Task<AdminCafeSettingsRequestResult> GetCafeSettingsAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetSettingsCallCount++;
            return Task.FromResult(AdminCafeSettingsRequestResult.Success(new AdminCafeSettingsResponse
            {
                Id = cafeId,
                Name = "Integration Cafe",
                Slug = "integration-cafe",
                IsActive = false,
                IsPublished = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }));
        }

        public Task<AdminCafeSettingsRequestResult> UpdateCafeSettingsAsync(
            long cafeId,
            AdminUpdateCafeSettingsRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCafeSettingsRequestResult.Failure());
        }

        public Task<AdminCafeSettingsRequestResult> ChangeCafePublicationAsync(
            long cafeId,
            AdminChangeCafePublicationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCafeSettingsRequestResult.Failure());
        }
    }

    private sealed class AdminPanelWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;
        private readonly IAdminCategoryApiClient _adminCategoryApiClient;
        private readonly IAdminProductApiClient _adminProductApiClient;
        private readonly IAdminBrandingApiClient _adminBrandingApiClient;
        private readonly IAdminCafeSettingsApiClient _adminCafeSettingsApiClient;

        public AdminPanelWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient,
            IAdminCategoryApiClient? adminCategoryApiClient = null,
            IAdminProductApiClient? adminProductApiClient = null,
            IAdminBrandingApiClient? adminBrandingApiClient = null,
            IAdminCafeSettingsApiClient? adminCafeSettingsApiClient = null)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
            _adminCategoryApiClient = adminCategoryApiClient ?? new FakeAdminCategoryApiClient();
            _adminProductApiClient = adminProductApiClient ?? new FakeAdminProductApiClient();
            _adminBrandingApiClient = adminBrandingApiClient ?? new FakeAdminBrandingApiClient();
            _adminCafeSettingsApiClient = adminCafeSettingsApiClient ?? new FakeAdminCafeSettingsApiClient();
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
                services.RemoveAll<IAdminCategoryApiClient>();
                services.AddSingleton(_adminCategoryApiClient);
                services.RemoveAll<IAdminProductApiClient>();
                services.AddSingleton(_adminProductApiClient);
                services.RemoveAll<IAdminBrandingApiClient>();
                services.AddSingleton(_adminBrandingApiClient);
                services.RemoveAll<IAdminCafeSettingsApiClient>();
                services.AddSingleton(_adminCafeSettingsApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-panel-shell-test-data-protection"));
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
}
