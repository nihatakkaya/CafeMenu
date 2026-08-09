using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Api.Common;
using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Tests;

public sealed class PublicMenuEndpointTests
{
    [Fact]
    public async Task PublishedActiveCafeSlug_ShouldReturnMenuWithoutAuthentication()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Public Cafe", "public-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Breakfast", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Toast", 1, 75m);
        await SeedThemeAsync(factory, cafe.Id);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(cafe.Name, json.RootElement.GetProperty("data").GetProperty("cafeName").GetString());
        Assert.Equal(cafe.Slug, json.RootElement.GetProperty("data").GetProperty("slug").GetString());
        Assert.Equal(category.Id, json.RootElement.GetProperty("data").GetProperty("categories")[0].GetProperty("id").GetInt64());
        Assert.Equal(product.Id, json.RootElement.GetProperty("data").GetProperty("categories")[0].GetProperty("products")[0].GetProperty("id").GetInt64());
        Assert.Equal(product.Price, json.RootElement.GetProperty("data").GetProperty("categories")[0].GetProperty("products")[0].GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task MissingSlug_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/PublicMenu/GetMenu/missing-slug");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InactiveCafe_ShouldNotReturnPublicMenu()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Inactive Public Cafe", "inactive-public-cafe", isActive: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnpublishedCafe_ShouldNotReturnPublicMenu()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Unpublished Public Cafe", "unpublished-public-cafe", isPublished: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeletedCafe_ShouldNotReturnPublicMenu()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Deleted Public Cafe", "deleted-public-cafe", isDeleted: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeletedCategoryAndProduct_ShouldNotAppear()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Soft Deleted Public Cafe", "soft-deleted-public-cafe");
        var visibleCategory = await SeedCategoryAsync(factory, cafe.Id, "Visible Category", 1);
        var deletedCategory = await SeedCategoryAsync(factory, cafe.Id, "Deleted Category", 2, isDeleted: true);
        var visibleProduct = await SeedProductAsync(factory, cafe.Id, visibleCategory.Id, "Visible Product", 1, 10m);
        var deletedProduct = await SeedProductAsync(factory, cafe.Id, visibleCategory.Id, "Deleted Product", 2, 20m, isDeleted: true);
        await SeedProductAsync(factory, cafe.Id, deletedCategory.Id, "Deleted Category Product", 1, 30m);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var categories = json.RootElement.GetProperty("data").GetProperty("categories").EnumerateArray().ToArray();
        var products = categories[0].GetProperty("products").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(categories, item => item.GetProperty("id").GetInt64() == deletedCategory.Id);
        Assert.Contains(products, item => item.GetProperty("id").GetInt64() == visibleProduct.Id);
        Assert.DoesNotContain(products, item => item.GetProperty("id").GetInt64() == deletedProduct.Id);
    }

    [Fact]
    public async Task InvisibleCategory_ShouldNotAppear()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Invisible Category Cafe", "invisible-category-cafe");
        await SeedCategoryAsync(factory, cafe.Id, "Hidden Category", 1, isVisible: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("categories").EnumerateArray());
    }

    [Fact]
    public async Task UnpublishedCategory_ShouldNotAppear()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Unpublished Category Cafe", "unpublished-category-cafe");
        await SeedCategoryAsync(factory, cafe.Id, "Draft Category", 1, isPublished: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(json.RootElement.GetProperty("data").GetProperty("categories").EnumerateArray());
    }

    [Fact]
    public async Task InvisibleProduct_ShouldNotAppear()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Invisible Product Cafe", "invisible-product-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Products", 1);
        await SeedProductAsync(factory, cafe.Id, category.Id, "Hidden Product", 1, 10m, isVisible: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var products = json.RootElement.GetProperty("data").GetProperty("categories")[0].GetProperty("products").EnumerateArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(products);
    }

    [Fact]
    public async Task UnpublishedProduct_ShouldNotAppear()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Unpublished Product Cafe", "unpublished-product-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Products", 1);
        await SeedProductAsync(factory, cafe.Id, category.Id, "Draft Product", 1, 10m, isPublished: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var products = json.RootElement.GetProperty("data").GetProperty("categories")[0].GetProperty("products").EnumerateArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(products);
    }

    [Fact]
    public async Task UnavailableProduct_ShouldRemainWithIsAvailableFalse()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Unavailable Product Cafe", "unavailable-product-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Products", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Sold Out", 1, 10m, isAvailable: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var responseProduct = json.RootElement.GetProperty("data").GetProperty("categories")[0].GetProperty("products")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(product.Id, responseProduct.GetProperty("id").GetInt64());
        Assert.False(responseProduct.GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public async Task OtherCafeData_ShouldNotLeakIntoMenu()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafeA = await SeedCafeAsync(factory, "Public Cafe A", "public-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Public Cafe B", "public-cafe-b");
        var categoryA = await SeedCategoryAsync(factory, cafeA.Id, "Cafe A Category", 1);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Cafe B Category", 1);
        var productA = await SeedProductAsync(factory, cafeA.Id, categoryA.Id, "Cafe A Product", 1, 10m);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Cafe B Product", 1, 20m);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafeA.Slug}");
        var json = await ParseAsync(response);
        var categories = json.RootElement.GetProperty("data").GetProperty("categories").EnumerateArray().ToArray();
        var products = categories.SelectMany(category => category.GetProperty("products").EnumerateArray()).ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(categories, item => item.GetProperty("id").GetInt64() == categoryA.Id);
        Assert.DoesNotContain(categories, item => item.GetProperty("id").GetInt64() == categoryB.Id);
        Assert.Contains(products, item => item.GetProperty("id").GetInt64() == productA.Id);
        Assert.DoesNotContain(products, item => item.GetProperty("id").GetInt64() == productB.Id);
    }

    [Fact]
    public async Task CategoriesAndProducts_ShouldBeOrderedByDisplayOrder()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Ordered Public Cafe", "ordered-public-cafe");
        var secondCategory = await SeedCategoryAsync(factory, cafe.Id, "Second Category", 2);
        var firstCategory = await SeedCategoryAsync(factory, cafe.Id, "First Category", 1);
        var secondProduct = await SeedProductAsync(factory, cafe.Id, firstCategory.Id, "Second Product", 2, 20m);
        var firstProduct = await SeedProductAsync(factory, cafe.Id, firstCategory.Id, "First Product", 1, 10m);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var categories = json.RootElement.GetProperty("data").GetProperty("categories").EnumerateArray().ToArray();
        var products = categories[0].GetProperty("products").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(firstCategory.Id, categories[0].GetProperty("id").GetInt64());
        Assert.Equal(secondCategory.Id, categories[1].GetProperty("id").GetInt64());
        Assert.Equal(firstProduct.Id, products[0].GetProperty("id").GetInt64());
        Assert.Equal(secondProduct.Id, products[1].GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task MissingTheme_ShouldReturnDefaultTheme()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Default Theme Cafe", "default-theme-cafe");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var theme = json.RootElement.GetProperty("data").GetProperty("theme");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CafeThemeConstants.DefaultPrimaryColor, theme.GetProperty("primaryColor").GetString());
        Assert.Equal(CafeThemeConstants.SystemFontPreset, theme.GetProperty("fontPreset").GetString());
        Assert.Equal(CafeThemeConstants.ClassicThemePreset, theme.GetProperty("themePreset").GetString());
    }

    private static async Task<CafeEntity> SeedCafeAsync(
        CustomWebApplicationFactory factory,
        string name,
        string slug,
        bool isActive = true,
        bool isPublished = true,
        bool isDeleted = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var cafe = new CafeEntity
        {
            Name = name,
            Slug = slug,
            LogoImageUrl = "https://cdn.example.com/logo.png",
            CoverImageUrl = "https://cdn.example.com/cover.png",
            IsActive = isActive,
            IsPublished = isPublished,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Cafes.Add(cafe);
        await dbContext.SaveChangesAsync();
        return cafe;
    }

    private static async Task<CategoryEntity> SeedCategoryAsync(
        CustomWebApplicationFactory factory,
        long cafeId,
        string name,
        int displayOrder,
        bool isVisible = true,
        bool isPublished = true,
        bool isDeleted = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var category = new CategoryEntity
        {
            CafeId = cafeId,
            Name = name,
            Description = "Public category",
            ImageUrl = "https://cdn.example.com/category.png",
            DisplayOrder = displayOrder,
            IsVisible = isVisible,
            IsPublished = isPublished,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    private static async Task<ProductEntity> SeedProductAsync(
        CustomWebApplicationFactory factory,
        long cafeId,
        long categoryId,
        string name,
        int displayOrder,
        decimal price,
        bool isVisible = true,
        bool isPublished = true,
        bool isDeleted = false,
        bool isAvailable = true)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var product = new ProductEntity
        {
            CafeId = cafeId,
            CategoryId = categoryId,
            Name = name,
            Description = "Public product",
            Price = price,
            ImageUrl = "https://cdn.example.com/product.png",
            DisplayOrder = displayOrder,
            IsVisible = isVisible,
            IsPublished = isPublished,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            IsAvailable = isAvailable,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    private static async Task SeedThemeAsync(CustomWebApplicationFactory factory, long cafeId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;

        dbContext.CafeThemes.Add(new CafeThemeEntity
        {
            CafeId = cafeId,
            PrimaryColor = "#123456",
            SecondaryColor = "#F5F5F5",
            AccentColor = "#D97706",
            BackgroundColor = "#FFFFFF",
            TextColor = "#111111",
            WelcomeTitle = "Welcome",
            WelcomeDescription = "Fresh menu",
            FontPreset = CafeThemeConstants.SansFontPreset,
            ThemePreset = CafeThemeConstants.ModernThemePreset,
            IsPublished = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
