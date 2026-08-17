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

public sealed class DashboardEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task CafeDashboard_ShouldReturnCategoryAndProductCounts()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "dashboard-owner@example.local");
        var cafe = await SeedCafeAsync(factory, "Dashboard Cafe", "dashboard-cafe", isPublished: true);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        await SeedCategoryAsync(factory, cafe.Id, isVisible: true, isPublished: true);
        await SeedCategoryAsync(factory, cafe.Id, isVisible: true, isPublished: false);
        await SeedCategoryAsync(factory, cafe.Id, isVisible: false, isPublished: true);
        await SeedProductAsync(factory, cafe.Id, isVisible: true, isPublished: true, isAvailable: true);
        await SeedProductAsync(factory, cafe.Id, isVisible: true, isPublished: false, isAvailable: true);
        await SeedProductAsync(factory, cafe.Id, isVisible: false, isPublished: true, isAvailable: false);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafe.Id}");
        var data = await ReadDataAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(cafe.Id, data.GetProperty("cafeId").GetInt64());
        Assert.Equal(cafe.Name, data.GetProperty("cafeName").GetString());
        Assert.True(data.GetProperty("isActive").GetBoolean());
        Assert.True(data.GetProperty("isPublished").GetBoolean());
        Assert.Equal(3, data.GetProperty("totalCategoryCount").GetInt32());
        Assert.Equal(1, data.GetProperty("publicCategoryCount").GetInt32());
        Assert.Equal(3, data.GetProperty("totalProductCount").GetInt32());
        Assert.Equal(1, data.GetProperty("publicProductCount").GetInt32());
        Assert.Equal(2, data.GetProperty("availableProductCount").GetInt32());
        Assert.Equal(1, data.GetProperty("unavailableProductCount").GetInt32());
    }

    [Fact]
    public async Task CafeDashboard_ShouldExcludeSoftDeletedRecords()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "dashboard-soft-delete@example.local");
        var cafe = await SeedCafeAsync(factory, "Soft Dashboard Cafe", "soft-dashboard-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        await SeedCategoryAsync(factory, cafe.Id);
        await SeedCategoryAsync(factory, cafe.Id, isDeleted: true);
        await SeedProductAsync(factory, cafe.Id);
        await SeedProductAsync(factory, cafe.Id, isDeleted: true);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafe.Id}");
        var data = await ReadDataAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, data.GetProperty("totalCategoryCount").GetInt32());
        Assert.Equal(1, data.GetProperty("publicCategoryCount").GetInt32());
        Assert.Equal(1, data.GetProperty("totalProductCount").GetInt32());
        Assert.Equal(1, data.GetProperty("publicProductCount").GetInt32());
    }

    [Fact]
    public async Task CafeDashboard_ShouldNotLeakCrossCafeStats()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "dashboard-cross-cafe@example.local");
        var cafeA = await SeedCafeAsync(factory, "Dashboard Cafe A", "dashboard-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Dashboard Cafe B", "dashboard-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        await SeedCategoryAsync(factory, cafeA.Id);
        await SeedProductAsync(factory, cafeA.Id);
        await SeedCategoryAsync(factory, cafeB.Id);
        await SeedCategoryAsync(factory, cafeB.Id);
        await SeedProductAsync(factory, cafeB.Id);
        await SeedProductAsync(factory, cafeB.Id);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var ownResponse = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafeA.Id}");
        var ownData = await ReadDataAsync(ownResponse);
        using var otherResponse = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafeB.Id}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(1, ownData.GetProperty("totalCategoryCount").GetInt32());
        Assert.Equal(1, ownData.GetProperty("totalProductCount").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Manager_ShouldReadOwnCafeDashboard()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "dashboard-manager@example.local");
        var cafe = await SeedCafeAsync(factory, "Manager Dashboard Cafe", "manager-dashboard-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        await SeedCategoryAsync(factory, cafe.Id);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafe.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_ShouldReadCafeDashboardWithoutMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "dashboard-platform@example.local", ApplicationRoles.PlatformAdmin);
        var cafe = await SeedCafeAsync(factory, "Platform Dashboard Cafe", "platform-dashboard-cafe", isActive: false);
        await SeedProductAsync(factory, cafe.Id);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafe.Id}");
        var data = await ReadDataAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(data.GetProperty("isActive").GetBoolean());
        Assert.Equal(1, data.GetProperty("totalProductCount").GetInt32());
    }

    [Fact]
    public async Task Anonymous_ShouldNotReadCafeDashboard()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Anonymous Dashboard Cafe", "anonymous-dashboard-cafe");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/Cafe/GetCafeDashboardStats/{cafe.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlatformDashboard_ShouldReturnCafeCounts()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "platform-stats@example.local", ApplicationRoles.PlatformAdmin);
        await SeedCafeAsync(factory, "Active Published", "active-published", isActive: true, isPublished: true);
        await SeedCafeAsync(factory, "Active Draft", "active-draft", isActive: true, isPublished: false);
        await SeedCafeAsync(factory, "Inactive Published", "inactive-published", isActive: false, isPublished: true);
        await SeedCafeAsync(factory, "Deleted Cafe", "deleted-cafe", isDeleted: true);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.GetAsync("/Cafe/GetPlatformDashboardStats");
        var data = await ReadDataAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, data.GetProperty("activeCafeCount").GetInt32());
        Assert.Equal(1, data.GetProperty("inactiveCafeCount").GetInt32());
        Assert.Equal(2, data.GetProperty("publishedCafeCount").GetInt32());
        Assert.Equal(1, data.GetProperty("draftCafeCount").GetInt32());
    }

    [Fact]
    public async Task NonPlatformUser_ShouldNotReadPlatformDashboard()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "non-platform-stats@example.local");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetPlatformDashboardStats");

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

    private static async Task<AppUserEntity> SeedUserAsync(
        CustomWebApplicationFactory factory,
        string email,
        params string[] roleCodes)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        EnsureRoles(dbContext);

        var utcNow = DateTimeOffset.UtcNow;
        var user = new AppUserEntity
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = "Dashboard User",
            PasswordHash = passwordHasher.HashPassword(ValidPassword),
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        foreach (var roleCode in roleCodes)
        {
            user.Roles.Add(dbContext.Roles.Single(role => role.Code == roleCode));
        }

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task<CafeEntity> SeedCafeAsync(
        CustomWebApplicationFactory factory,
        string name,
        string slug,
        bool isActive = true,
        bool isPublished = false,
        bool isDeleted = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var cafe = new CafeEntity
        {
            Name = name,
            Slug = slug,
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

    private static async Task SeedCategoryAsync(
        CustomWebApplicationFactory factory,
        long cafeId,
        bool isVisible = true,
        bool isPublished = true,
        bool isDeleted = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;

        dbContext.Categories.Add(new CategoryEntity
        {
            CafeId = cafeId,
            Name = $"Category {Guid.NewGuid():N}",
            DisplayOrder = 1,
            IsVisible = isVisible,
            IsPublished = isPublished,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedProductAsync(
        CustomWebApplicationFactory factory,
        long cafeId,
        bool isVisible = true,
        bool isPublished = true,
        bool isAvailable = true,
        bool isDeleted = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var category = dbContext.Categories.FirstOrDefault(category => category.CafeId == cafeId && !category.IsDeleted)
            ?? new CategoryEntity
            {
                CafeId = cafeId,
                Name = $"Product Category {Guid.NewGuid():N}",
                DisplayOrder = 1,
                IsVisible = true,
                IsPublished = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

        if (category.Id == 0)
        {
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        dbContext.Products.Add(new ProductEntity
        {
            CafeId = cafeId,
            CategoryId = category.Id,
            Name = $"Product {Guid.NewGuid():N}",
            Price = 10m,
            DisplayOrder = 1,
            IsVisible = isVisible,
            IsPublished = isPublished,
            IsAvailable = isAvailable,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedMembershipAsync(
        CustomWebApplicationFactory factory,
        long appUserId,
        long cafeId,
        string roleCode)
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
            IsActive = true,
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

    private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var json = await ParseAsync(response);
        return json.RootElement.GetProperty("data");
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
