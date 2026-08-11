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

public sealed class CafeTenantAuthorizationEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task PlatformAdmin_ShouldCreateCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "admin@example.com", ApplicationRoles.PlatformAdmin);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.PostAsJsonAsync(
            "/Cafe/CreateCafe",
            new CreateCafeRequest { Name = "Central Cafe", Slug = "Central-Cafe" });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("central-cafe", json.RootElement.GetProperty("data").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task NormalUser_ShouldNotCreateCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var user = await SeedUserAsync(factory, "user@example.com");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, user.Email);

        using var response = await client.PostAsJsonAsync(
            "/Cafe/CreateCafe",
            new CreateCafeRequest { Name = "Unauthorized Cafe", Slug = "unauthorized-cafe" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CafeOwner_ShouldReadOwnCafePrivateDetails()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Owner Cafe", "owner-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeById/{cafe.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CafeOwner_ShouldNotReadAnotherCafePrivateDetails()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "owner-a@example.com");
        var cafeA = await SeedCafeAsync(factory, "Cafe A", "cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Cafe B", "cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeById/{cafeB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CafeOwner_ShouldNotUpdateAnotherCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "update-owner@example.com");
        var cafeA = await SeedCafeAsync(factory, "Update Cafe A", "update-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Update Cafe B", "update-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Cafe/UpdateCafe/{cafeB.Id}",
            new UpdateCafeRequest { Name = "Blocked Update", Slug = "blocked-update" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CafeManager_ShouldAccessOnlyMembershipCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "manager@example.com");
        var cafeA = await SeedCafeAsync(factory, "Manager Cafe A", "manager-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Manager Cafe B", "manager-cafe-b");
        await SeedMembershipAsync(factory, manager.Id, cafeA.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var ownResponse = await client.GetAsync($"/Cafe/GetCafeById/{cafeA.Id}");
        using var otherResponse = await client.GetAsync($"/Cafe/GetCafeById/{cafeB.Id}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task ClientSuppliedCafeId_ShouldNotBypassTenantIsolation()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "bypass-owner@example.com");
        var ownedCafe = await SeedCafeAsync(factory, "Owned Cafe", "owned-cafe");
        var targetCafe = await SeedCafeAsync(factory, "Target Cafe", "target-cafe");
        await SeedMembershipAsync(factory, owner.Id, ownedCafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Cafe/UpdateCafe/{targetCafe.Id}",
            new UpdateCafeRequest { Name = "Bypass Attempt", Slug = "bypass-attempt" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivatedMembership_ShouldNotGrantAccess()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "inactive-membership@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Membership Cafe", "inactive-membership-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner, isActive: false);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeById/{cafe.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivatedCafe_ShouldBlockPrivateMembershipOperations()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "inactive-cafe@example.com");
        var cafe = await SeedCafeAsync(factory, "Inactive Cafe", "inactive-cafe", isActive: false);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeById/{cafe.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignCafeOwner_ShouldBeIdempotentForExistingOwnerMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "membership-admin@example.com", ApplicationRoles.PlatformAdmin);
        var owner = await SeedUserAsync(factory, "membership-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Membership Cafe", "membership-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        var request = new AssignCafeOwnerRequest { CafeId = cafe.Id, AppUserId = owner.Id };
        using var firstResponse = await client.PostAsJsonAsync("/Cafe/AssignCafeOwner", request);
        using var secondResponse = await client.PostAsJsonAsync("/Cafe/AssignCafeOwner", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCafe_ShouldRejectDuplicateSlugCaseInsensitively()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "slug-admin@example.com", ApplicationRoles.PlatformAdmin);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var firstResponse = await client.PostAsJsonAsync(
            "/Cafe/CreateCafe",
            new CreateCafeRequest { Name = "Slug Cafe", Slug = "Slug-Cafe" });
        using var secondResponse = await client.PostAsJsonAsync(
            "/Cafe/CreateCafe",
            new CreateCafeRequest { Name = "Slug Cafe Duplicate", Slug = "slug-cafe" });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_ShouldListActivateAndDeactivateCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "manage-admin@example.com", ApplicationRoles.PlatformAdmin);
        var cafe = await SeedCafeAsync(factory, "Managed Cafe", "managed-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var listResponse = await client.GetAsync("/Cafe/GetCafes");
        using var deactivateResponse = await client.PutAsync($"/Cafe/DeactivateCafe/{cafe.Id}", null);
        using var activateResponse = await client.PutAsync($"/Cafe/ActivateCafe/{cafe.Id}", null);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
    }

    [Fact]
    public async Task CafeOwner_ShouldPublishAndUnpublishOwnCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "publish-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Publish Cafe", "publish-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var publishResponse = await client.PutAsJsonAsync(
            $"/Cafe/ChangeCafePublication/{cafe.Id}",
            new ChangeCafePublicationRequest { IsPublished = true });
        var publishJson = await ParseAsync(publishResponse);

        using var unpublishResponse = await client.PutAsJsonAsync(
            $"/Cafe/ChangeCafePublication/{cafe.Id}",
            new ChangeCafePublicationRequest { IsPublished = false });
        var unpublishJson = await ParseAsync(unpublishResponse);

        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        Assert.True(publishJson.RootElement.GetProperty("data").GetProperty("isPublished").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);
        Assert.False(unpublishJson.RootElement.GetProperty("data").GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task CafeOwner_ShouldNotPublishAnotherCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "publish-bypass-owner@example.com");
        var cafeA = await SeedCafeAsync(factory, "Publish Bypass Cafe A", "publish-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Publish Bypass Cafe B", "publish-bypass-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Cafe/ChangeCafePublication/{cafeB.Id}",
            new ChangeCafePublicationRequest { IsPublished = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_ShouldNotPublishSoftDeletedCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(factory, "publish-deleted-admin@example.com", ApplicationRoles.PlatformAdmin);
        var cafe = await SeedCafeAsync(factory, "Deleted Publish Cafe", "deleted-publish-cafe", isDeleted: true);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.PutAsJsonAsync(
            $"/Cafe/ChangeCafePublication/{cafe.Id}",
            new ChangeCafePublicationRequest { IsPublished = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        bool isActive = true,
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
            IsPublished = false,
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
