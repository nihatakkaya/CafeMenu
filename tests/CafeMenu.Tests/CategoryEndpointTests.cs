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

public sealed class CategoryEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task Owner_ShouldCreateCategoryForOwnCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Owner Category Cafe", "owner-category-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Category/CreateCategory",
            new CreateCategoryRequest { CafeId = cafe.Id, Name = "Breakfast", DisplayOrder = 1 });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(cafe.Id, json.RootElement.GetProperty("data").GetProperty("cafeId").GetInt64());
        Assert.Equal("Breakfast", json.RootElement.GetProperty("data").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Manager_ShouldCreateCategoryForOwnCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "category-manager@example.com");
        var cafe = await SeedCafeAsync(factory, "Manager Category Cafe", "manager-category-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.PostAsJsonAsync(
            "/Category/CreateCategory",
            new CreateCategoryRequest { CafeId = cafe.Id, Name = "Drinks", DisplayOrder = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotCreateCategoryForAnotherCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-cross-create@example.com");
        var cafeA = await SeedCafeAsync(factory, "Category Cafe A", "category-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Category Cafe B", "category-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Category/CreateCategory",
            new CreateCategoryRequest { CafeId = cafeB.Id, Name = "Blocked", DisplayOrder = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotReadAnotherCafeCategory()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-cross-read@example.com");
        var cafeA = await SeedCafeAsync(factory, "Read Cafe A", "read-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Read Cafe B", "read-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Private B", 1);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Category/GetCategoryById/{categoryB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotUpdateOrDeleteAnotherCafeCategory()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-cross-write@example.com");
        var cafeA = await SeedCafeAsync(factory, "Write Cafe A", "write-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Write Cafe B", "write-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Private Write B", 1);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/Category/UpdateCategory/{categoryB.Id}",
            new UpdateCategoryRequest { CafeId = cafeB.Id, Name = "Blocked Update", DisplayOrder = 3 });
        using var deleteResponse = await client.DeleteAsync($"/Category/DeleteCategory/{categoryB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task InactiveMembership_ShouldNotAccessCategoryManagement()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-inactive-membership@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Membership Category Cafe", "inactive-membership-category-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner, isActive: false);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Category/CreateCategory",
            new CreateCategoryRequest { CafeId = cafe.Id, Name = "Blocked", DisplayOrder = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InactiveCafe_ShouldBlockCategoryManagement()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-inactive-cafe@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Category Cafe", "inactive-category-cafe", isActive: false);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Category/CreateCategory",
            new CreateCategoryRequest { CafeId = cafe.Id, Name = "Blocked", DisplayOrder = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeletedCategory_ShouldNotAppearInNormalList()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-soft-delete@example.com");
        var cafe = await SeedCafeAsync(factory, "Soft Delete Cafe", "soft-delete-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        var category = await SeedCategoryAsync(factory, cafe.Id, "Hidden Later", 1);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var deleteResponse = await client.DeleteAsync($"/Category/DeleteCategory/{category.Id}");
        using var listResponse = await client.GetAsync($"/Category/GetCategories/{cafe.Id}");
        var json = await ParseAsync(listResponse);
        var categories = json.RootElement.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.DoesNotContain(categories, item => item.GetProperty("id").GetInt64() == category.Id);
    }

    [Fact]
    public async Task ChangeCategoryVisibility_ShouldUpdateVisibility()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "category-visibility@example.com");
        var cafe = await SeedCafeAsync(factory, "Visibility Cafe", "visibility-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        var category = await SeedCategoryAsync(factory, cafe.Id, "Visible Category", 1);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Category/ChangeCategoryVisibility/{category.Id}",
            new ChangeCategoryVisibilityRequest { CafeId = cafe.Id, IsVisible = false });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(json.RootElement.GetProperty("data").GetProperty("isVisible").GetBoolean());
    }

    [Fact]
    public async Task ReorderCategories_ShouldUpdateOnlySameCafeCategories()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-reorder@example.com");
        var cafe = await SeedCafeAsync(factory, "Reorder Cafe", "reorder-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        var first = await SeedCategoryAsync(factory, cafe.Id, "First", 1);
        var second = await SeedCategoryAsync(factory, cafe.Id, "Second", 2);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            "/Category/ReorderCategories",
            new ReorderCategoriesRequest
            {
                CafeId = cafe.Id,
                Categories =
                [
                    new CategoryOrderRequest { CategoryId = first.Id, DisplayOrder = 2 },
                    new CategoryOrderRequest { CategoryId = second.Id, DisplayOrder = 1 }
                ]
            });
        var json = await ParseAsync(response);
        var categories = json.RootElement.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(second.Id, categories[0].GetProperty("id").GetInt64());
        Assert.Equal(1, categories[0].GetProperty("displayOrder").GetInt32());
        Assert.Equal(first.Id, categories[1].GetProperty("id").GetInt64());
        Assert.Equal(2, categories[1].GetProperty("displayOrder").GetInt32());
    }

    [Fact]
    public async Task ReorderCategories_ShouldRejectAnotherCafeCategoryId()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-reorder-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Reorder Bypass Cafe A", "reorder-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Reorder Bypass Cafe B", "reorder-bypass-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        var categoryA = await SeedCategoryAsync(factory, cafeA.Id, "Allowed", 1);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Blocked", 2);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            "/Category/ReorderCategories",
            new ReorderCategoriesRequest
            {
                CafeId = cafeA.Id,
                Categories =
                [
                    new CategoryOrderRequest { CategoryId = categoryA.Id, DisplayOrder = 2 },
                    new CategoryOrderRequest { CategoryId = categoryB.Id, DisplayOrder = 1 }
                ]
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ShouldRejectCafeIdBypass()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "category-cafeid-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "CafeId Bypass Cafe A", "cafeid-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "CafeId Bypass Cafe B", "cafeid-bypass-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Bypass Target", 1);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Category/UpdateCategory/{categoryB.Id}",
            new UpdateCategoryRequest { CafeId = cafeA.Id, Name = "Bypass", DisplayOrder = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
