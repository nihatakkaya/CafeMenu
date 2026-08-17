extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminBranding;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminCategory;
using CafeMenuWeb::CafeMenu.Web.AdminImageUpload;
using CafeMenuWeb::CafeMenu.Web.AdminProduct;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AdminImageUploadBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public async Task BrandingImageForms_ShouldRenderMultipartPostsWithExactlyOneAntiforgeryToken()
    {
        await using var factory = new AdminImageUploadWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");
        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = await response.Content.ReadAsStringAsync();

        AssertPostForm(html, "/admin/cafes/10/branding/upload-logo", isMultipart: true);
        AssertPostForm(html, "/admin/cafes/10/branding/remove-logo", isMultipart: false);
        AssertPostForm(html, "/admin/cafes/10/branding/upload-cover", isMultipart: true);
        AssertPostForm(html, "/admin/cafes/10/branding/remove-cover", isMultipart: false);
    }

    [Fact]
    public async Task CategoryImageForms_ShouldRenderMultipartPostsWithExactlyOneAntiforgeryToken()
    {
        await using var factory = new AdminImageUploadWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");
        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        AssertPostForm(html, "/admin/cafes/10/categories/21/upload-image", isMultipart: true);
        AssertPostForm(html, "/admin/cafes/10/categories/21/remove-image", isMultipart: false);
    }

    [Fact]
    public async Task ProductImageForms_ShouldRenderMultipartPostsWithExactlyOneAntiforgeryToken()
    {
        await using var factory = new AdminImageUploadWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");
        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();

        AssertPostForm(html, "/admin/cafes/10/products/31/upload-image", isMultipart: true);
        AssertPostForm(html, "/admin/cafes/10/products/31/remove-image", isMultipart: false);
    }

    [Fact]
    public async Task AdminImageUploadApiClient_ShouldUseAuthenticatedAdminHttpClientAndSemanticEndpoints()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK),
            new HttpResponseMessage(HttpStatusCode.OK),
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var httpClientFactory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminImageUploadApiClient(httpClientFactory);

        var uploadStatus = await apiClient.UploadCafeLogoAsync(
            10,
            CreateFormFile("logo.png", "image/png"),
            CancellationToken.None);
        var removeStatus = await apiClient.RemoveCategoryImageAsync(21, CancellationToken.None);
        var validationStatus = await apiClient.UploadProductImageAsync(
            31,
            CreateFormFile("invalid.gif", "image/gif"),
            CancellationToken.None);
        var failureStatus = await apiClient.RemoveCafeCoverAsync(10, CancellationToken.None);

        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, httpClientFactory.LastClientName);
        Assert.Equal(AdminImageUploadStatus.Success, uploadStatus);
        Assert.Equal(AdminImageUploadStatus.Success, removeStatus);
        Assert.Equal(AdminImageUploadStatus.ValidationError, validationStatus);
        Assert.Equal(AdminImageUploadStatus.Failure, failureStatus);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.example.test/CafeBranding/UploadLogoImage/10", request.Uri);
                Assert.Contains("name=File", request.Body ?? string.Empty, StringComparison.Ordinal);
                Assert.Contains("filename=logo.png", request.Body ?? string.Empty, StringComparison.Ordinal);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.example.test/Category/RemoveCategoryImage/21", request.Uri);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.example.test/Product/UploadProductImage/31", request.Uri);
                Assert.Contains("name=File", request.Body ?? string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain("RoleId", request.Body ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("RoleCode", request.Body ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.example.test/CafeBranding/RemoveCoverImage/10", request.Uri);
            });
    }

    private static IFormFile CreateFormFile(string fileName, string contentType)
    {
        var stream = new MemoryStream([1, 2, 3, 4]);
        return new FormFile(stream, 0, stream.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static void AssertPostForm(string html, string action, bool isMultipart)
    {
        var form = FindFormByAction(html, action);
        Assert.Contains("method=\"post\"", form, StringComparison.OrdinalIgnoreCase);

        if (isMultipart)
        {
            Assert.Contains("enctype=\"multipart/form-data\"", form, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("type=\"file\"", form, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("name=\"File\"", form, StringComparison.Ordinal);
            Assert.Contains("accept=\"image/jpeg,image/png,image/webp\"", form, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("enctype=\"multipart/form-data\"", form, StringComparison.OrdinalIgnoreCase);
        }

        var tokenCount = Regex.Matches(
            form,
            "name=\"__RequestVerificationToken\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
        Assert.Equal(1, tokenCount);
    }

    private static string FindFormByAction(string html, string action)
    {
        var forms = Regex.Matches(
            html,
            "<form(?<attrs>[^>]*)>(?<body>.*?)</form>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (Match form in forms)
        {
            if (form.Value.Contains($"action=\"{action}\"", StringComparison.OrdinalIgnoreCase))
            {
                return form.Value;
            }
        }

        throw new InvalidOperationException($"Form action '{action}' was not found.\n{html}");
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

    private sealed class AdminImageUploadWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthApiClient>();
                services.AddSingleton<IAdminAuthApiClient>(new FakeAdminAuthApiClient(CreateAuthResponse()));
                services.RemoveAll<IAdminCafeApiClient>();
                services.AddSingleton<IAdminCafeApiClient>(new FakeAdminCafeApiClient());
                services.RemoveAll<IAdminBrandingApiClient>();
                services.AddSingleton<IAdminBrandingApiClient>(new FakeAdminBrandingApiClient());
                services.RemoveAll<IAdminCategoryApiClient>();
                services.AddSingleton<IAdminCategoryApiClient>(new FakeAdminCategoryApiClient());
                services.RemoveAll<IAdminProductApiClient>();
                services.AddSingleton<IAdminProductApiClient>(new FakeAdminProductApiClient());
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-image-upload-test-data-protection"));
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }
    }

    private sealed class FakeAdminCafeApiClient : IAdminCafeApiClient
    {
        public Task<AdminCafeListResult> GetMyCafesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCafeListResult.Success([
                new AdminCafeResponse
                {
                    Id = 10,
                    Name = "Image Upload Cafe",
                    Slug = "image-upload-cafe",
                    LogoImageUrl = "https://cdn.example.test/logo.png",
                    IsActive = true,
                    IsPublished = true,
                    RoleCodes = [ "CAFE_OWNER" ]
                }
            ]));
        }

        public Task<AdminCafeDashboardStatsResult> GetCafeDashboardStatsAsync(
            long cafeId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCafeDashboardStatsResult.Failure());
        }
    }

    private sealed class FakeAdminBrandingApiClient : IAdminBrandingApiClient
    {
        public Task<AdminBrandingRequestResult> GetCafeBrandingAsync(long cafeId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminBrandingRequestResult.Success(new AdminBrandingResponse
            {
                CafeId = cafeId,
                CafeName = "Image Upload Cafe",
                LogoImageUrl = "https://cdn.example.test/logo.png",
                CoverImageUrl = "https://cdn.example.test/cover.png",
                PrimaryColor = "#111827",
                SecondaryColor = "#F9FAFB",
                AccentColor = "#D97706",
                BackgroundColor = "#FFFFFF",
                TextColor = "#111827",
                WelcomeTitle = "Welcome",
                WelcomeDescription = "Fresh menu selections",
                FontPreset = AdminBrandingConstants.SystemFontPreset,
                ThemePreset = AdminBrandingConstants.ClassicThemePreset,
                IsPublished = true,
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

    private sealed class FakeAdminCategoryApiClient : IAdminCategoryApiClient
    {
        public Task<AdminCategoryListResult> GetCategoriesAsync(long cafeId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryListResult.Success([
                new AdminCategoryResponse
                {
                    Id = 21,
                    CafeId = cafeId,
                    Name = "Desserts",
                    Description = "Daily desserts",
                    ImageUrl = "https://cdn.example.test/category.png",
                    DisplayOrder = 1,
                    IsVisible = true,
                    IsPublished = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]));
        }

        public Task<AdminCategoryMutationResult> CreateCategoryAsync(
            AdminCreateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryMutationResult> UpdateCategoryAsync(
            long categoryId,
            AdminUpdateCategoryRequest request,
            CancellationToken cancellationToken)
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
        public Task<AdminProductListResult> GetProductsAsync(long cafeId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductListResult.Success([
                new AdminProductResponse
                {
                    Id = 31,
                    CafeId = cafeId,
                    CategoryId = 21,
                    Name = "Cheesecake",
                    Description = "Daily dessert",
                    ImageUrl = "https://cdn.example.test/product.png",
                    Price = 150m,
                    DisplayOrder = 1,
                    IsVisible = true,
                    IsAvailable = true,
                    IsPublished = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]));
        }

        public Task<AdminProductMutationResult> CreateProductAsync(
            AdminCreateProductRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminProductMutationResult.Failure());
        }

        public Task<AdminProductMutationResult> UpdateProductAsync(
            long productId,
            AdminUpdateProductRequest request,
            CancellationToken cancellationToken)
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

    private sealed class FakeAdminAuthApiClient : IAdminAuthApiClient
    {
        private readonly AdminAuthResponse? _loginResponse;

        public FakeAdminAuthApiClient(AdminAuthResponse? loginResponse)
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

    private sealed record RecordedRequest(HttpMethod Method, string? Uri, string? Body, string BodyHeaders);

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var bodyHeaders = request.Content is null
                ? string.Empty
                : string.Join("\n", request.Content.Headers.Select(header => $"{header.Key}={string.Join(",", header.Value)}"));

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.ToString(), body, bodyHeaders));

            return _responses.Dequeue();
        }
    }
}
