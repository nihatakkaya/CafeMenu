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

public sealed class CafeGetMyCafesEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task GetMyCafes_ShouldRequireAuthentication()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/Cafe/GetMyCafes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyCafes_ShouldReturnOnlyOwnerActiveCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "owner-my-cafes@example.com");
        var ownCafe = await SeedCafeAsync(factory, "Owner Cafe", "owner-cafe", logoImageUrl: "logos/owner.png");
        var otherCafe = await SeedCafeAsync(factory, "Other Cafe", "other-cafe");
        await SeedMembershipAsync(factory, owner.Id, ownCafe.Id, ApplicationRoles.CafeOwner);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cafe = Assert.Single(cafes.EnumerateArray());
        AssertCafe(cafe, ownCafe.Id, "Owner Cafe", "owner-cafe", "logos/owner.png", isActive: true, isPublished: false);
        AssertRoleCodes(cafe, ApplicationRoles.CafeOwner);
        Assert.DoesNotContain(cafes.EnumerateArray(), item => item.GetProperty("id").GetInt64() == otherCafe.Id);
    }

    [Fact]
    public async Task GetMyCafes_ShouldReturnOnlyManagerActiveCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "manager-my-cafes@example.com");
        var cafe = await SeedCafeAsync(factory, "Manager Cafe", "manager-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        var cafeJson = Assert.Single(cafes.EnumerateArray());
        Assert.Equal(cafe.Id, cafeJson.GetProperty("id").GetInt64());
        AssertRoleCodes(cafeJson, ApplicationRoles.CafeManager);
    }

    [Fact]
    public async Task GetMyCafes_ShouldReturnDifferentCafeScopedRolesForSameUser()
    {
        await using var factory = new CustomWebApplicationFactory();
        var user = await SeedUserAsync(factory, "multi-role-cafes@example.com");
        var cafeA = await SeedCafeAsync(factory, "Alpha Cafe", "alpha-cafe");
        var cafeB = await SeedCafeAsync(factory, "Beta Cafe", "beta-cafe");
        await SeedMembershipAsync(factory, user.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        await SeedMembershipAsync(factory, user.Id, cafeB.Id, ApplicationRoles.CafeManager);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, user.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);
        var cafeItems = cafes.EnumerateArray().ToArray();

        Assert.Equal(2, cafeItems.Length);
        AssertRoleCodes(cafeItems.Single(item => item.GetProperty("id").GetInt64() == cafeA.Id), ApplicationRoles.CafeOwner);
        AssertRoleCodes(cafeItems.Single(item => item.GetProperty("id").GetInt64() == cafeB.Id), ApplicationRoles.CafeManager);
    }

    [Fact]
    public async Task GetMyCafes_ShouldNotReturnAnotherUsersCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var user = await SeedUserAsync(factory, "visible-user@example.com");
        var otherUser = await SeedUserAsync(factory, "hidden-user@example.com");
        var visibleCafe = await SeedCafeAsync(factory, "Visible Cafe", "visible-cafe");
        var hiddenCafe = await SeedCafeAsync(factory, "Hidden Cafe", "hidden-cafe");
        await SeedMembershipAsync(factory, user.Id, visibleCafe.Id, ApplicationRoles.CafeOwner);
        await SeedMembershipAsync(factory, otherUser.Id, hiddenCafe.Id, ApplicationRoles.CafeOwner);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, user.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Contains(cafes.EnumerateArray(), item => item.GetProperty("id").GetInt64() == visibleCafe.Id);
        Assert.DoesNotContain(cafes.EnumerateArray(), item => item.GetProperty("id").GetInt64() == hiddenCafe.Id);
    }

    [Fact]
    public async Task GetMyCafes_ShouldIgnoreInactiveMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "inactive-membership-list@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Membership Cafe", "inactive-membership-list-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner, isActive: false);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Empty(cafes.EnumerateArray());
    }

    [Fact]
    public async Task GetMyCafes_ShouldIgnoreSoftDeletedMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "deleted-membership-list@example.com");
        var cafe = await SeedCafeAsync(factory, "Deleted Membership Cafe", "deleted-membership-list-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner, isDeleted: true);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Empty(cafes.EnumerateArray());
    }

    [Fact]
    public async Task GetMyCafes_ShouldHideInactiveCafeForOwnerOrManager()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "inactive-cafe-list@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Cafe", "inactive-cafe-list", isActive: false);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Empty(cafes.EnumerateArray());
    }

    [Fact]
    public async Task GetMyCafes_ShouldHideSoftDeletedCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "deleted-cafe-list@example.com");
        var cafe = await SeedCafeAsync(factory, "Deleted Cafe", "deleted-cafe-list", isDeleted: true);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Empty(cafes.EnumerateArray());
    }

    [Fact]
    public async Task GetMyCafes_ShouldReturnAllNonDeletedCafesForPlatformAdminWithoutMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "platform-my-cafes@example.com", ApplicationRoles.PlatformAdmin);
        var activeCafe = await SeedCafeAsync(factory, "Active Cafe", "active-platform-cafe", isPublished: true);
        var inactiveCafe = await SeedCafeAsync(factory, "Inactive Cafe", "inactive-platform-cafe", isActive: false);
        var deletedCafe = await SeedCafeAsync(factory, "Deleted Cafe", "deleted-platform-cafe", isDeleted: true);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);
        var cafeItems = cafes.EnumerateArray().ToArray();

        Assert.Equal(2, cafeItems.Length);
        Assert.Contains(cafeItems, item => item.GetProperty("id").GetInt64() == activeCafe.Id);
        Assert.Contains(cafeItems, item => item.GetProperty("id").GetInt64() == inactiveCafe.Id);
        Assert.DoesNotContain(cafeItems, item => item.GetProperty("id").GetInt64() == deletedCafe.Id);
        Assert.All(cafeItems, item => AssertRoleCodes(item, ApplicationRoles.PlatformAdmin));
        Assert.False(cafeItems.Single(item => item.GetProperty("id").GetInt64() == inactiveCafe.Id).GetProperty("isActive").GetBoolean());
        Assert.True(cafeItems.Single(item => item.GetProperty("id").GetInt64() == activeCafe.Id).GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task GetMyCafes_ShouldNotTrustGlobalCafeOwnerRoleWithoutMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var user = await SeedUserAsync(factory, "global-owner-no-membership@example.com", ApplicationRoles.CafeOwner);
        await SeedCafeAsync(factory, "Unassigned Cafe", "unassigned-cafe");

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, user.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);

        Assert.Empty(cafes.EnumerateArray());
    }

    [Fact]
    public async Task GetMyCafes_ShouldReturnDuplicateCafeOnlyOnce()
    {
        await using var factory = new CustomWebApplicationFactory();
        var user = await SeedUserAsync(factory, "duplicate-cafe-list@example.com");
        var cafe = await SeedCafeAsync(factory, "Duplicate Cafe", "duplicate-cafe-list");
        await SeedMembershipAsync(factory, user.Id, cafe.Id, ApplicationRoles.CafeOwner);
        await SeedMembershipAsync(factory, user.Id, cafe.Id, ApplicationRoles.CafeManager);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, user.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);
        var cafeJson = Assert.Single(cafes.EnumerateArray());

        Assert.Equal(cafe.Id, cafeJson.GetProperty("id").GetInt64());
        AssertRoleCodes(cafeJson, ApplicationRoles.CafeManager, ApplicationRoles.CafeOwner);
    }

    [Fact]
    public async Task GetMyCafes_ShouldNotExposeEntityFields()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "entity-exposure-list@example.com");
        var cafe = await SeedCafeAsync(factory, "Entity Exposure Cafe", "entity-exposure-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);

        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync("/Cafe/GetMyCafes");
        var cafes = await ReadCafeArrayAsync(response);
        var cafeJson = Assert.Single(cafes.EnumerateArray());

        Assert.True(cafeJson.TryGetProperty("id", out _));
        Assert.True(cafeJson.TryGetProperty("name", out _));
        Assert.True(cafeJson.TryGetProperty("slug", out _));
        Assert.True(cafeJson.TryGetProperty("logoImageUrl", out _));
        Assert.True(cafeJson.TryGetProperty("isActive", out _));
        Assert.True(cafeJson.TryGetProperty("isPublished", out _));
        Assert.True(cafeJson.TryGetProperty("roleCodes", out _));
        Assert.False(cafeJson.TryGetProperty("createdAt", out _));
        Assert.False(cafeJson.TryGetProperty("updatedAt", out _));
        Assert.False(cafeJson.TryGetProperty("isDeleted", out _));
        Assert.False(cafeJson.TryGetProperty("deletedAt", out _));
        Assert.False(cafeJson.TryGetProperty("memberships", out _));
        Assert.False(cafeJson.TryGetProperty("passwordHash", out _));
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

    private static async Task<JsonElement> ReadCafeArrayAsync(HttpResponseMessage response)
    {
        var json = await ParseAsync(response);
        return json.RootElement.GetProperty("data").Clone();
    }

    private static void AssertCafe(
        JsonElement cafe,
        long id,
        string name,
        string slug,
        string? logoImageUrl,
        bool isActive,
        bool isPublished)
    {
        Assert.Equal(id, cafe.GetProperty("id").GetInt64());
        Assert.Equal(name, cafe.GetProperty("name").GetString());
        Assert.Equal(slug, cafe.GetProperty("slug").GetString());
        Assert.Equal(logoImageUrl, cafe.GetProperty("logoImageUrl").GetString());
        Assert.Equal(isActive, cafe.GetProperty("isActive").GetBoolean());
        Assert.Equal(isPublished, cafe.GetProperty("isPublished").GetBoolean());
    }

    private static void AssertRoleCodes(JsonElement cafe, params string[] expectedRoleCodes)
    {
        var roleCodes = cafe
            .GetProperty("roleCodes")
            .EnumerateArray()
            .Select(roleCode => roleCode.GetString())
            .ToArray();

        Assert.Equal(expectedRoleCodes.Order(StringComparer.Ordinal), roleCodes.Order(StringComparer.Ordinal));
    }

    private static async Task<AppUserEntity> SeedUserAsync(
        CustomWebApplicationFactory factory,
        string email,
        params string[] platformRoles)
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

        foreach (var roleCode in platformRoles)
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
        string? logoImageUrl = null,
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
            LogoImageUrl = logoImageUrl,
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

    private static async Task SeedMembershipAsync(
        CustomWebApplicationFactory factory,
        long appUserId,
        long cafeId,
        string roleCode,
        bool isActive = true,
        bool isDeleted = false)
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
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
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
