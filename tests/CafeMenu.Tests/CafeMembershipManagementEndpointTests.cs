using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Api.Data;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Tests;

public sealed class CafeMembershipManagementEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public void MembershipModel_ShouldAllowOnlyOneActiveUserCafeMembership()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var entityType = dbContext.Model.FindEntityType(typeof(CafeMembershipEntity));
        var uniqueIndex = entityType?.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(CafeMembershipEntity.AppUserId),
                nameof(CafeMembershipEntity.CafeId)
            ]));

        Assert.NotNull(uniqueIndex);
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal("is_active = true AND is_deleted = false", uniqueIndex.GetFilter());
    }

    [Fact]
    public async Task PlatformAdmin_ShouldAssignActiveUserAsOwner()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "owner-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var user = await SeedUserAsync(factory, "new-owner@example.local");
        var cafe = await SeedCafeAsync(factory, "Owner Assign Cafe", "owner-assign-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var response = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeOwner",
            new AssignCafeOwnerRequest { CafeId = cafe.Id, AppUserId = user.Id });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApplicationRoles.CafeOwner, json.RootElement.GetProperty("data").GetProperty("roleCode").GetString());
        Assert.Equal(1, await CountActiveMembershipsAsync(factory, user.Id, cafe.Id));
        Assert.Empty(await GetGlobalCafeRolesAsync(factory, user.Id));
    }

    [Fact]
    public async Task AssignCafeOwner_ShouldRejectPendingAndDeletedUsers()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "blocked-owner-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var pendingUser = await SeedUserAsync(factory, "pending-owner@example.local", isActive: false);
        var deletedUser = await SeedUserAsync(factory, "deleted-owner@example.local", isDeleted: true);
        var cafe = await SeedCafeAsync(factory, "Blocked Owner Cafe", "blocked-owner-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var pendingResponse = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeOwner",
            new AssignCafeOwnerRequest { CafeId = cafe.Id, AppUserId = pendingUser.Id });
        using var deletedResponse = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeOwner",
            new AssignCafeOwnerRequest { CafeId = cafe.Id, AppUserId = deletedUser.Id });

        Assert.Equal(HttpStatusCode.NotFound, pendingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_ShouldAssignManager()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "manager-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var user = await SeedUserAsync(factory, "new-manager@example.local");
        var cafe = await SeedCafeAsync(factory, "Manager Assign Cafe", "manager-assign-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var response = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = user.Id });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApplicationRoles.CafeManager, json.RootElement.GetProperty("data").GetProperty("roleCode").GetString());
        Assert.Empty(await GetGlobalCafeRolesAsync(factory, user.Id));
    }

    [Fact]
    public async Task CafeOwner_ShouldAssignManagerOnlyForOwnCafe()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "owner-manager-admin@example.local");
        var manager = await SeedUserAsync(factory, "owner-created-manager@example.local");
        var cafeA = await SeedCafeAsync(factory, "Owner Manager Cafe A", "owner-manager-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Owner Manager Cafe B", "owner-manager-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var ownCafeResponse = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafeA.Id, AppUserId = manager.Id });
        using var otherCafeResponse = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafeB.Id, AppUserId = manager.Id });

        Assert.Equal(HttpStatusCode.OK, ownCafeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherCafeResponse.StatusCode);
    }

    [Fact]
    public async Task CafeManagerAndNormalUser_ShouldNotManageMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "membership-manager@example.local");
        var normalUser = await SeedUserAsync(factory, "membership-normal@example.local");
        var target = await SeedUserAsync(factory, "membership-target@example.local");
        var cafe = await SeedCafeAsync(factory, "Manager Forbidden Cafe", "manager-forbidden-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var managerClient = factory.CreateClient();
        using var normalClient = factory.CreateClient();
        await AuthorizeAsync(managerClient, manager.Email);
        await AuthorizeAsync(normalClient, normalUser.Email);

        using var managerResponse = await managerClient.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = target.Id });
        using var normalResponse = await normalClient.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = target.Id });

        Assert.Equal(HttpStatusCode.Forbidden, managerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, normalResponse.StatusCode);
    }

    [Fact]
    public async Task AssignCafeManager_ShouldRejectPendingUser()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "pending-manager-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var pendingUser = await SeedUserAsync(factory, "pending-manager@example.local", isActive: false);
        var cafe = await SeedCafeAsync(factory, "Pending Manager Cafe", "pending-manager-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var response = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = pendingUser.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateActiveMembership_ShouldNotCreateDuplicateRows()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "duplicate-membership-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var user = await SeedUserAsync(factory, "duplicate-membership-user@example.local");
        var cafe = await SeedCafeAsync(factory, "Duplicate Membership Cafe", "duplicate-membership-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        var request = new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = user.Id };
        using var firstResponse = await client.PostAsJsonAsync("/Cafe/AssignCafeManager", request);
        using var secondResponse = await client.PostAsJsonAsync("/Cafe/AssignCafeManager", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(1, await CountActiveMembershipsAsync(factory, user.Id, cafe.Id));
    }

    [Fact]
    public async Task AssignOwnerAndManager_ShouldChangeExistingRole()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "role-change-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var user = await SeedUserAsync(factory, "role-change-user@example.local");
        var cafe = await SeedCafeAsync(factory, "Role Change Cafe", "role-change-cafe");
        var membership = await SeedMembershipAsync(factory, user.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var ownerResponse = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeOwner",
            new AssignCafeOwnerRequest { CafeId = cafe.Id, AppUserId = user.Id });
        var ownerJson = await ParseAsync(ownerResponse);

        using var managerResponse = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = user.Id });
        var managerJson = await ParseAsync(managerResponse);

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(membership.Id, ownerJson.RootElement.GetProperty("data").GetProperty("id").GetInt64());
        Assert.Equal(ApplicationRoles.CafeOwner, ownerJson.RootElement.GetProperty("data").GetProperty("roleCode").GetString());
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
        Assert.Equal(membership.Id, managerJson.RootElement.GetProperty("data").GetProperty("id").GetInt64());
        Assert.Equal(ApplicationRoles.CafeManager, managerJson.RootElement.GetProperty("data").GetProperty("roleCode").GetString());
        Assert.Equal(1, await CountActiveMembershipsAsync(factory, user.Id, cafe.Id));
    }

    [Fact]
    public async Task CafeOwner_ShouldNotDemoteOwnerToManager()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "owner-demote-admin@example.local");
        var otherOwner = await SeedUserAsync(factory, "owner-demote-target@example.local");
        var cafe = await SeedCafeAsync(factory, "Owner Demote Cafe", "owner-demote-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        await SeedMembershipAsync(factory, otherOwner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsJsonAsync(
            "/Cafe/AssignCafeManager",
            new AssignCafeManagerRequest { CafeId = cafe.Id, AppUserId = otherOwner.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCafeMembers_ShouldReturnMinimalActiveMemberData()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "member-list-owner@example.local");
        var manager = await SeedUserAsync(factory, "member-list-manager@example.local");
        var cafe = await SeedCafeAsync(factory, "Member List Cafe", "member-list-cafe");
        var ownerMembership = await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/Cafe/GetCafeMembers/{cafe.Id}");
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var members = json.RootElement.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, members.Length);
        Assert.Contains(members, member => member.GetProperty("membershipId").GetInt64() == ownerMembership.Id);
        Assert.All(members, member =>
        {
            Assert.True(member.TryGetProperty("membershipId", out _));
            Assert.True(member.TryGetProperty("appUserId", out _));
            Assert.True(member.TryGetProperty("email", out _));
            Assert.True(member.TryGetProperty("fullName", out _));
            Assert.True(member.TryGetProperty("roleCode", out _));
            Assert.True(member.TryGetProperty("isActive", out _));
        });
        Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCafeMembers_ShouldRejectCrossTenantAndManagerAccess()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "member-cross-owner@example.local");
        var manager = await SeedUserAsync(factory, "member-cross-manager@example.local");
        var cafeA = await SeedCafeAsync(factory, "Member Cross Cafe A", "member-cross-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Member Cross Cafe B", "member-cross-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        await SeedMembershipAsync(factory, manager.Id, cafeA.Id, ApplicationRoles.CafeManager);
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();
        await AuthorizeAsync(ownerClient, owner.Email);
        await AuthorizeAsync(managerClient, manager.Email);

        using var crossTenantResponse = await ownerClient.GetAsync($"/Cafe/GetCafeMembers/{cafeB.Id}");
        using var managerResponse = await managerClient.GetAsync($"/Cafe/GetCafeMembers/{cafeA.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, crossTenantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, managerResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivateCafeMembership_ShouldRemoveCafeFromGetMyCafes()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "deactivate-owner@example.local");
        var manager = await SeedUserAsync(factory, "deactivate-manager@example.local");
        var cafe = await SeedCafeAsync(factory, "Deactivate Cafe", "deactivate-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        var managerMembership = await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();
        await AuthorizeAsync(ownerClient, owner.Email);
        await AuthorizeAsync(managerClient, manager.Email);

        using var deactivateResponse = await ownerClient.PostAsync($"/Cafe/DeactivateCafeMembership/{managerMembership.Id}", null);
        using var secondDeactivateResponse = await ownerClient.PostAsync($"/Cafe/DeactivateCafeMembership/{managerMembership.Id}", null);
        using var myCafesResponse = await managerClient.GetAsync("/Cafe/GetMyCafes");
        var myCafesJson = await ParseAsync(myCafesResponse);
        var cafes = myCafesJson.RootElement.GetProperty("data").EnumerateArray();

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondDeactivateResponse.StatusCode);
        Assert.Empty(cafes);
    }

    [Fact]
    public async Task DeactivateCafeMembership_ShouldEnforceRoleRules()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "deactivate-admin@example.local", [ApplicationRoles.PlatformAdmin]);
        var owner = await SeedUserAsync(factory, "deactivate-owner-rule@example.local");
        var manager = await SeedUserAsync(factory, "deactivate-manager-rule@example.local");
        var cafe = await SeedCafeAsync(factory, "Deactivate Rule Cafe", "deactivate-rule-cafe");
        var ownerMembership = await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        var managerMembership = await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var adminClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();
        await AuthorizeAsync(adminClient, admin.Email);
        await AuthorizeAsync(managerClient, manager.Email);

        using var managerResponse = await managerClient.PostAsync($"/Cafe/DeactivateCafeMembership/{ownerMembership.Id}", null);
        using var adminResponse = await adminClient.PostAsync($"/Cafe/DeactivateCafeMembership/{managerMembership.Id}", null);

        Assert.Equal(HttpStatusCode.Forbidden, managerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
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
        IReadOnlyCollection<string>? roleCodes = null,
        bool isActive = true,
        bool isDeleted = false)
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
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        foreach (var roleCode in roleCodes ?? [])
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
        string slug)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var cafe = new CafeEntity
        {
            Name = name,
            Slug = slug,
            IsActive = true,
            IsPublished = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Cafes.Add(cafe);
        await dbContext.SaveChangesAsync();
        return cafe;
    }

    private static async Task<CafeMembershipEntity> SeedMembershipAsync(
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
        var membership = new CafeMembershipEntity
        {
            AppUserId = appUserId,
            CafeId = cafeId,
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.CafeMemberships.Add(membership);
        await dbContext.SaveChangesAsync();
        return membership;
    }

    private static async Task<int> CountActiveMembershipsAsync(
        CustomWebApplicationFactory factory,
        long appUserId,
        long cafeId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        return await dbContext.CafeMemberships.CountAsync(membership =>
            membership.AppUserId == appUserId &&
            membership.CafeId == cafeId &&
            membership.IsActive &&
            !membership.IsDeleted);
    }

    private static async Task<IReadOnlyCollection<string>> GetGlobalCafeRolesAsync(
        CustomWebApplicationFactory factory,
        long appUserId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var user = await dbContext.AppUsers
            .Include(existingUser => existingUser.Roles)
            .SingleAsync(existingUser => existingUser.Id == appUserId);

        return user.Roles
            .Select(role => role.Code)
            .Where(roleCode => roleCode is ApplicationRoles.CafeOwner or ApplicationRoles.CafeManager)
            .ToArray();
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
