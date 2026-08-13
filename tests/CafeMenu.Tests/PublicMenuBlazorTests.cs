extern alias CafeMenuWeb;

using System.Net;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class PublicMenuBlazorTests
{
    [Fact]
    public async Task PublicMenuPage_ShouldCallApiWithSlug()
    {
        var apiClient = new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenu()));
        await using var factory = new PublicMenuWebApplicationFactory(apiClient);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, html);
        Assert.Equal("mocca-cafe", apiClient.LastSlug);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldRenderSuccessfulMenu()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenu())));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mocca Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Kahveler", html, StringComparison.Ordinal);
        Assert.Contains("Flat White", html, StringComparison.Ordinal);
        Assert.Contains("150,00", html, StringComparison.Ordinal);
        Assert.Contains("&#x20BA;", html, StringComparison.Ordinal);
        Assert.Contains("/c/mocca-cafe/products/20", html, StringComparison.Ordinal);
        Assert.Contains("name=\"q\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldFilterProductsByName()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenuWithMultipleProducts())));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe?q=latte");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Arama", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Latte", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Flat White", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldFilterProductsCaseInsensitivelyByDescription()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenuWithMultipleProducts())));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe?q=ESPRESSO");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Flat White", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Latte", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldKeepCategoryMenuForEmptySearch()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenuWithMultipleProducts())));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe?q=");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("category-nav", html, StringComparison.Ordinal);
        Assert.Contains("Flat White", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Latte", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-state=\"no-search-results\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldRenderNoResultSearchState()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenuWithMultipleProducts())));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe?q=matchnothing");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-state=\"no-search-results\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Flat White", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Cafe Latte", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldKeepUnavailableProductVisible()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(CreateMenu(isAvailable: false))));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Flat White", html, StringComparison.Ordinal);
        Assert.Contains("availability", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldApplyThemeThroughSafeCssVariables()
    {
        var menu = CreateMenu(theme: new PublicMenuThemeResponse
            {
                PrimaryColor = "#123456",
                SecondaryColor = "javascript:alert(1)",
                AccentColor = "#D97706",
                BackgroundColor = "#FFFFFF",
                TextColor = "#111111",
                FontPreset = "SANS",
                ThemePreset = "MODERN"
            });
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Success(menu)));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("--cafe-primary-color: #123456", html, StringComparison.Ordinal);
        Assert.Contains("theme-modern", html, StringComparison.Ordinal);
        Assert.Contains("font-sans", html, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript:alert", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldRenderNotFoundState()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.NotFound()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/missing-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("public-menu-state", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMenuPage_ShouldRenderApiFailureState()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(PublicMenuRequestResult.Failure()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("public-menu-state", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicProductDetailPage_ShouldCallApiWithSlugAndProductId()
    {
        var apiClient = new FakePublicMenuApiClient(
            PublicMenuRequestResult.NotFound(),
            PublicProductDetailRequestResult.Success(CreateProductDetail()));
        await using var factory = new PublicMenuWebApplicationFactory(apiClient);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe/products/20");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("mocca-cafe", apiClient.LastProductDetailSlug);
        Assert.Equal(20, apiClient.LastProductDetailProductId);
        Assert.Contains("Flat White", html, StringComparison.Ordinal);
        Assert.Contains("Kahveler", html, StringComparison.Ordinal);
        Assert.Contains("150,00", html, StringComparison.Ordinal);
        Assert.Contains("/c/mocca-cafe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicProductDetailPage_ShouldRenderUnavailableState()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(
                PublicMenuRequestResult.NotFound(),
                PublicProductDetailRequestResult.Success(CreateProductDetail(isAvailable: false))));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe/products/20");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("availability", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicProductDetailPage_ShouldRenderNotFoundState()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(
                PublicMenuRequestResult.NotFound(),
                PublicProductDetailRequestResult.NotFound()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe/products/999");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-state=\"not-found\"", html, StringComparison.Ordinal);
        Assert.Contains("/c/mocca-cafe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicProductDetailPage_ShouldRenderFailureState()
    {
        await using var factory = new PublicMenuWebApplicationFactory(
            new FakePublicMenuApiClient(
                PublicMenuRequestResult.NotFound(),
                PublicProductDetailRequestResult.Failure()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/c/mocca-cafe/products/20");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-state=\"failure\"", html, StringComparison.Ordinal);
    }

    private static PublicMenuResponse CreateMenu(
        bool isAvailable = true,
        PublicMenuThemeResponse? theme = null)
    {
        return new PublicMenuResponse
        {
            CafeName = "Mocca Cafe",
            Slug = "mocca-cafe",
            LogoImageUrl = "https://cdn.example.com/logo.png",
            CoverImageUrl = "https://cdn.example.com/cover.png",
            Theme = theme ?? new PublicMenuThemeResponse
            {
                PrimaryColor = "#111827",
                SecondaryColor = "#F9FAFB",
                AccentColor = "#D97706",
                BackgroundColor = "#FFFFFF",
                TextColor = "#111111",
                WelcomeTitle = "Welcome",
                WelcomeDescription = "Daily menu",
                FontPreset = "SYSTEM",
                ThemePreset = "CLASSIC"
            },
            Categories =
            [
                new PublicMenuCategoryResponse
                {
                    Id = 10,
                    Name = "Kahveler",
                    Description = "Hot drinks",
                    DisplayOrder = 1,
                    Products =
                    [
                        new PublicMenuProductResponse
                        {
                            Id = 20,
                            Name = "Flat White",
                            Description = "Double espresso",
                            Price = 150m,
                            ImageUrl = "https://cdn.example.com/flat-white.png",
                            IsAvailable = isAvailable,
                            DisplayOrder = 1
                        }
                    ]
                }
            ]
        };
    }

    private static PublicMenuResponse CreateMenuWithMultipleProducts()
    {
        return new PublicMenuResponse
        {
            CafeName = "Mocca Cafe",
            Slug = "mocca-cafe",
            LogoImageUrl = "https://cdn.example.com/logo.png",
            CoverImageUrl = "https://cdn.example.com/cover.png",
            Theme = new PublicMenuThemeResponse
            {
                PrimaryColor = "#111827",
                SecondaryColor = "#F9FAFB",
                AccentColor = "#D97706",
                BackgroundColor = "#FFFFFF",
                TextColor = "#111111",
                WelcomeTitle = "Welcome",
                WelcomeDescription = "Daily menu",
                FontPreset = "SYSTEM",
                ThemePreset = "CLASSIC"
            },
            Categories =
            [
                new PublicMenuCategoryResponse
                {
                    Id = 10,
                    Name = "Kahveler",
                    Description = "Hot drinks",
                    DisplayOrder = 1,
                    Products =
                    [
                        new PublicMenuProductResponse
                        {
                            Id = 20,
                            Name = "Flat White",
                            Description = "Double espresso",
                            Price = 150m,
                            ImageUrl = "https://cdn.example.com/flat-white.png",
                            IsAvailable = true,
                            DisplayOrder = 1
                        },
                        new PublicMenuProductResponse
                        {
                            Id = 21,
                            Name = "Cafe Latte",
                            Description = "Espresso and milk",
                            Price = 140m,
                            ImageUrl = "https://cdn.example.com/latte.png",
                            IsAvailable = true,
                            DisplayOrder = 2
                        }
                    ]
                }
            ]
        };
    }

    private static PublicProductDetailResponse CreateProductDetail(bool isAvailable = true)
    {
        return new PublicProductDetailResponse
        {
            CafeName = "Mocca Cafe",
            Slug = "mocca-cafe",
            LogoImageUrl = "https://cdn.example.com/logo.png",
            CoverImageUrl = "https://cdn.example.com/cover.png",
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
            CategoryId = 10,
            CategoryName = "Kahveler",
            ProductId = 20,
            ProductName = "Flat White",
            Description = "Double espresso",
            Price = 150m,
            ImageUrl = "https://cdn.example.com/flat-white.png",
            IsAvailable = isAvailable
        };
    }

    private sealed class FakePublicMenuApiClient : IPublicMenuApiClient
    {
        private readonly PublicMenuRequestResult _result;
        private readonly PublicProductDetailRequestResult _productDetailResult;

        public FakePublicMenuApiClient(
            PublicMenuRequestResult result,
            PublicProductDetailRequestResult? productDetailResult = null)
        {
            _result = result;
            _productDetailResult = productDetailResult ?? PublicProductDetailRequestResult.NotFound();
        }

        public string? LastSlug { get; private set; }

        public string? LastProductDetailSlug { get; private set; }

        public long? LastProductDetailProductId { get; private set; }

        public Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
        {
            LastSlug = slug;
            return Task.FromResult(_result);
        }

        public Task<PublicProductDetailRequestResult> GetProductDetailAsync(
            string slug,
            long productId,
            CancellationToken cancellationToken)
        {
            LastProductDetailSlug = slug;
            LastProductDetailProductId = productId;
            return Task.FromResult(_productDetailResult);
        }
    }

    private sealed class PublicMenuWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IPublicMenuApiClient _publicMenuApiClient;

        public PublicMenuWebApplicationFactory(IPublicMenuApiClient publicMenuApiClient)
        {
            _publicMenuApiClient = publicMenuApiClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton(_publicMenuApiClient);
            });
        }
    }
}
