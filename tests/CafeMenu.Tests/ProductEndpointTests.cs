using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Api.Data;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Tests;

public sealed class ProductEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task Owner_ShouldCreateProductForOwnCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Owner Product Cafe", "owner-product-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Meals", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafe.Id, category.Id, "Toast", 85.50m));
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(cafe.Id, json.RootElement.GetProperty("data").GetProperty("cafeId").GetInt64());
        Assert.Equal(category.Id, json.RootElement.GetProperty("data").GetProperty("categoryId").GetInt64());
        Assert.Equal(85.50m, json.RootElement.GetProperty("data").GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task Manager_ShouldCreateProductForOwnCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "product-manager@example.com");
        var cafe = await SeedCafeAsync(factory, "Manager Product Cafe", "manager-product-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Drinks", 1);
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafe.Id, category.Id, "Tea", 30m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotCreateProductForAnotherCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-cross-create@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Cafe A", "product-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Cafe B", "product-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Blocked", 1);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafeB.Id, categoryB.Id, "Blocked Product", 10m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotCreateProductWithAnotherCafeCategoryId()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-category-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Category Bypass Cafe A", "category-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Category Bypass Cafe B", "category-bypass-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Foreign Category", 1);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafeA.Id, categoryB.Id, "Bypass Product", 10m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotReadAnotherCafeProduct()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-cross-read@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Read Cafe A", "product-read-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Read Cafe B", "product-read-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Private B", 1);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Read Target", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Product/GetProductById/{productB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotUpdateAnotherCafeProduct()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-cross-update@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Update Cafe A", "product-update-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Update Cafe B", "product-update-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Private Update", 1);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Update Target", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/UpdateProduct/{productB.Id}",
            UpdateProductRequest(cafeB.Id, categoryB.Id, "Blocked Update", 30m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotDeleteAnotherCafeProduct()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-cross-delete@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Delete Cafe A", "product-delete-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Delete Cafe B", "product-delete-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Private Delete", 1);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Delete Target", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.DeleteAsync($"/Product/DeleteProduct/{productB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProductId_ShouldNotBypassTenantIsolation()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-id-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Id Cafe A", "product-id-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Id Cafe B", "product-id-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Product Id Target", 1);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Product Id Target", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/ChangeProductVisibility/{productB.Id}",
            new ChangeProductVisibilityRequest { CafeId = cafeA.Id, IsVisible = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ShouldRejectAnotherCafeCategoryId()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-category-change-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Category Cafe A", "product-category-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Category Cafe B", "product-category-cafe-b");
        var categoryA = await SeedCategoryAsync(factory, cafeA.Id, "Allowed Category", 1);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Foreign Category", 1);
        var productA = await SeedProductAsync(factory, cafeA.Id, categoryA.Id, "Own Product", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/UpdateProduct/{productA.Id}",
            UpdateProductRequest(cafeA.Id, categoryB.Id, "Bypass Category", 30m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InactiveMembership_ShouldNotAccessProductManagement()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-inactive-membership@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Product Membership Cafe", "inactive-product-membership-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Inactive Membership Category", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner, isActive: false);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafe.Id, category.Id, "Blocked", 10m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InactiveCafe_ShouldBlockProductManagement()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-inactive-cafe@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Product Cafe", "inactive-product-cafe", isActive: false);
        var category = await SeedCategoryAsync(factory, cafe.Id, "Inactive Cafe Category", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafe.Id, category.Id, "Blocked", 10m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ShouldRejectNegativePrice()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-negative-price@example.com");
        var cafe = await SeedCafeAsync(factory, "Negative Price Cafe", "negative-price-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Negative Price Category", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Product/CreateProduct",
            CreateProductRequest(cafe.Id, category.Id, "Invalid Price", -1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeletedProduct_ShouldNotAppearInNormalList()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-soft-delete@example.com");
        var cafe = await SeedCafeAsync(factory, "Product Soft Delete Cafe", "product-soft-delete-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Soft Delete Category", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Hidden Later", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var deleteResponse = await client.DeleteAsync($"/Product/DeleteProduct/{product.Id}");
        using var listResponse = await client.GetAsync($"/Product/GetProducts/{cafe.Id}");
        var json = await ParseAsync(listResponse);
        var products = json.RootElement.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.DoesNotContain(products, item => item.GetProperty("id").GetInt64() == product.Id);
    }

    [Fact]
    public async Task ChangeProductVisibility_ShouldUpdateVisibility()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "product-visibility@example.com");
        var cafe = await SeedCafeAsync(factory, "Product Visibility Cafe", "product-visibility-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Visibility Category", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Visible Product", 1, 25m);
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/ChangeProductVisibility/{product.Id}",
            new ChangeProductVisibilityRequest { CafeId = cafe.Id, IsVisible = false });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(json.RootElement.GetProperty("data").GetProperty("isVisible").GetBoolean());
    }

    [Fact]
    public async Task ChangeProductAvailability_ShouldUpdateAvailability()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "product-availability@example.com");
        var cafe = await SeedCafeAsync(factory, "Product Availability Cafe", "product-availability-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Availability Category", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Available Product", 1, 25m);
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/ChangeProductAvailability/{product.Id}",
            new ChangeProductAvailabilityRequest { CafeId = cafe.Id, IsAvailable = false });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(json.RootElement.GetProperty("data").GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public async Task ChangeProductPublication_ShouldPublishAndUnpublishProduct()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "product-publication@example.com");
        var cafe = await SeedCafeAsync(factory, "Product Publication Cafe", "product-publication-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Product Publication Category", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Publication Product", 1, 25m);
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var publishResponse = await client.PutAsJsonAsync(
            $"/Product/ChangeProductPublication/{product.Id}",
            new ChangeProductPublicationRequest { CafeId = cafe.Id, IsPublished = true });
        var publishJson = await ParseAsync(publishResponse);

        using var unpublishResponse = await client.PutAsJsonAsync(
            $"/Product/ChangeProductPublication/{product.Id}",
            new ChangeProductPublicationRequest { CafeId = cafe.Id, IsPublished = false });
        var unpublishJson = await ParseAsync(unpublishResponse);

        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        Assert.True(publishJson.RootElement.GetProperty("data").GetProperty("isPublished").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);
        Assert.False(unpublishJson.RootElement.GetProperty("data").GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task ChangeProductPublication_ShouldRejectCafeIdBypass()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-publication-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Publication Bypass Cafe A", "product-publication-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Publication Bypass Cafe B", "product-publication-bypass-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Blocked Publication Category", 1);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Blocked Publication Product", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/ChangeProductPublication/{productB.Id}",
            new ChangeProductPublicationRequest { CafeId = cafeA.Id, IsPublished = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeProductPublication_ShouldNotPublishSoftDeletedProduct()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-publication-deleted@example.com");
        var cafe = await SeedCafeAsync(factory, "Deleted Product Publication Cafe", "deleted-product-publication-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Deleted Product Publication Category", 1);
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Deleted Publication Product", 1, 25m, isDeleted: true);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Product/ChangeProductPublication/{product.Id}",
            new ChangeProductPublicationRequest { CafeId = cafe.Id, IsPublished = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReorderProducts_ShouldUpdateSameCafeCategoryProducts()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-reorder@example.com");
        var cafe = await SeedCafeAsync(factory, "Product Reorder Cafe", "product-reorder-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Reorder Category", 1);
        var first = await SeedProductAsync(factory, cafe.Id, category.Id, "First Product", 1, 10m);
        var second = await SeedProductAsync(factory, cafe.Id, category.Id, "Second Product", 2, 20m);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            "/Product/ReorderProducts",
            new ReorderProductsRequest
            {
                CafeId = cafe.Id,
                CategoryId = category.Id,
                Products =
                [
                    new ProductOrderRequest { ProductId = first.Id, DisplayOrder = 2 },
                    new ProductOrderRequest { ProductId = second.Id, DisplayOrder = 1 }
                ]
            });
        var json = await ParseAsync(response);
        var products = json.RootElement.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(second.Id, products[0].GetProperty("id").GetInt64());
        Assert.Equal(1, products[0].GetProperty("displayOrder").GetInt32());
        Assert.Equal(first.Id, products[1].GetProperty("id").GetInt64());
        Assert.Equal(2, products[1].GetProperty("displayOrder").GetInt32());
    }

    [Fact]
    public async Task ReorderProducts_ShouldRejectAnotherCafeProductId()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "product-reorder-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Product Reorder Bypass Cafe A", "product-reorder-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Product Reorder Bypass Cafe B", "product-reorder-bypass-cafe-b");
        var categoryA = await SeedCategoryAsync(factory, cafeA.Id, "Allowed Reorder", 1);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Blocked Reorder", 1);
        var productA = await SeedProductAsync(factory, cafeA.Id, categoryA.Id, "Allowed Product", 1, 10m);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Blocked Product", 2, 20m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            "/Product/ReorderProducts",
            new ReorderProductsRequest
            {
                CafeId = cafeA.Id,
                CategoryId = categoryA.Id,
                Products =
                [
                    new ProductOrderRequest { ProductId = productA.Id, DisplayOrder = 2 },
                    new ProductOrderRequest { ProductId = productB.Id, DisplayOrder = 1 }
                ]
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static CreateProductRequest CreateProductRequest(long cafeId, long categoryId, string name, decimal price)
    {
        return new CreateProductRequest
        {
            CafeId = cafeId,
            CategoryId = categoryId,
            Name = name,
            Description = "Test product",
            Price = price,
            DisplayOrder = 1,
            IsAvailable = true,
            IsVisible = true
        };
    }

    private static UpdateProductRequest UpdateProductRequest(long cafeId, long categoryId, string name, decimal price)
    {
        return new UpdateProductRequest
        {
            CafeId = cafeId,
            CategoryId = categoryId,
            Name = name,
            Description = "Updated product",
            Price = price,
            DisplayOrder = 1
        };
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/Authentication/Login",
            new LoginRequest
            {
                Email = email,
                Password = ValidPassword
            });
        var json = await ParseAsync(response);
        var accessToken = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<AppUserEntity> SeedUserAsync(CustomWebApplicationFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        EnsureRoles(dbContext);

        var utcNow = DateTimeOffset.UtcNow;
        var user = new AppUserEntity
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = "Test User",
            PasswordHash = passwordHasher.HashPassword(ValidPassword),
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task<CafeEntity> SeedCafeAsync(
        CustomWebApplicationFactory factory,
        string name,
        string slug,
        bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var cafe = new CafeEntity
        {
            Name = name,
            Slug = slug,
            IsActive = isActive,
            IsPublished = false,
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
        int displayOrder)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var category = new CategoryEntity
        {
            CafeId = cafeId,
            Name = name,
            DisplayOrder = displayOrder,
            IsVisible = true,
            IsPublished = false,
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
        bool isDeleted = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var product = new ProductEntity
        {
            CafeId = cafeId,
            CategoryId = categoryId,
            Name = name,
            Price = price,
            DisplayOrder = displayOrder,
            IsAvailable = true,
            IsVisible = true,
            IsPublished = false,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    private static async Task SeedMembershipAsync(
        CustomWebApplicationFactory factory,
        long appUserId,
        long cafeId,
        string roleCode,
        bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        EnsureRoles(dbContext);
        var role = dbContext.Roles.Single(existingRole => existingRole.Code == roleCode);
        var utcNow = DateTimeOffset.UtcNow;

        dbContext.CafeMemberships.Add(new CafeMembershipEntity
        {
            AppUserId = appUserId,
            CafeId = cafeId,
            RoleId = role.Id,
            IsActive = isActive,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static void EnsureRoles(CafeMenuDbContext dbContext)
    {
        if (dbContext.Roles.Any())
        {
            return;
        }

        var utcNow = DateTimeOffset.UtcNow;
        dbContext.Roles.AddRange(
            new RoleEntity
            {
                Id = 1,
                Code = ApplicationRoles.PlatformAdmin,
                Name = "Platform Administrator",
                Description = "Manages platform-level administration.",
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            },
            new RoleEntity
            {
                Id = 2,
                Code = ApplicationRoles.CafeOwner,
                Name = "Cafe Owner",
                Description = "Cafe-scoped owner role reserved for membership authorization.",
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            },
            new RoleEntity
            {
                Id = 3,
                Code = ApplicationRoles.CafeManager,
                Name = "Cafe Manager",
                Description = "Cafe-scoped manager role reserved for membership authorization.",
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        dbContext.SaveChanges();
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
