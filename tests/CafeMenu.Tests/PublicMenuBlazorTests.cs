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
        Assert.Contains("Tükendi", html, StringComparison.Ordinal);
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
        Assert.Contains("Menü bulunamadı", html, StringComparison.Ordinal);
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
        Assert.Contains("Menü yüklenemedi", html, StringComparison.Ordinal);
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
                WelcomeTitle = "Hoş geldiniz",
                WelcomeDescription = "Taze kahveler ve günlük lezzetler",
                FontPreset = "SYSTEM",
                ThemePreset = "CLASSIC"
            },
            Categories =
            [
                new PublicMenuCategoryResponse
                {
                    Id = 10,
                    Name = "Kahveler",
                    Description = "Sıcak içecekler",
                    DisplayOrder = 1,
                    Products =
                    [
                        new PublicMenuProductResponse
                        {
                            Id = 20,
                            Name = "Flat White",
                            Description = "Çift shot espresso",
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

    private sealed class FakePublicMenuApiClient : IPublicMenuApiClient
    {
        private readonly PublicMenuRequestResult _result;

        public FakePublicMenuApiClient(PublicMenuRequestResult result)
        {
            _result = result;
        }

        public string? LastSlug { get; private set; }

        public Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
        {
            LastSlug = slug;
            return Task.FromResult(_result);
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
