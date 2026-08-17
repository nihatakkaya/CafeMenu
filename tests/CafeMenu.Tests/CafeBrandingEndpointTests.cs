using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Api.Common;
using CafeMenu.Api.Data;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Tests;

public sealed class CafeBrandingEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task Owner_ShouldReadOwnCafeBranding()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-read-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Read Cafe", "branding-read-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/CafeBranding/GetCafeBranding/{cafe.Id}");
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(cafe.Id, json.RootElement.GetProperty("data").GetProperty("cafeId").GetInt64());
        Assert.Equal(CafeThemeConstants.ClassicThemePreset, json.RootElement.GetProperty("data").GetProperty("themePreset").GetString());
    }

    [Fact]
    public async Task Owner_ShouldUpdateOwnCafeBranding()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-update-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Update Cafe", "branding-update-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest(themePreset: CafeThemeConstants.ModernThemePreset));
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://cdn.example.com/logo.png", json.RootElement.GetProperty("data").GetProperty("logoImageUrl").GetString());
        Assert.Equal(CafeThemeConstants.ModernThemePreset, json.RootElement.GetProperty("data").GetProperty("themePreset").GetString());
    }

    [Fact]
    public async Task Manager_ShouldUpdateOwnCafeBranding()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "branding-update-manager@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Manager Cafe", "branding-manager-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest(themePreset: CafeThemeConstants.CompactThemePreset));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_ShouldUpdateActiveCafeBrandingWithoutMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(
            factory,
            "branding-platform-admin@example.com",
            platformRoleCode: ApplicationRoles.PlatformAdmin);
        var cafe = await SeedCafeAsync(factory, "Branding Platform Cafe", "branding-platform-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ShouldNotUpdateAnotherCafeBranding()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-cross-update@example.com");
        var cafeA = await SeedCafeAsync(factory, "Branding Cafe A", "branding-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Branding Cafe B", "branding-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafeB.Id}",
            ValidRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CafeId_ShouldNotBypassTenantIsolation()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-cafeid-bypass@example.com");
        var cafeA = await SeedCafeAsync(factory, "Branding Bypass Cafe A", "branding-bypass-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Branding Bypass Cafe B", "branding-bypass-cafe-b");
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.GetAsync($"/CafeBranding/GetCafeBranding/{cafeB.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InactiveMembership_ShouldNotAccessCafeBranding()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-inactive-membership@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Inactive Membership Cafe", "branding-inactive-membership-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner, isActive: false);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InactiveCafe_ShouldBlockBrandingManagement()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-inactive-cafe@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Inactive Cafe", "branding-inactive-cafe", isActive: false);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidThemePreset_ShouldBeRejected()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-invalid-theme@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Invalid Theme Cafe", "branding-invalid-theme-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest(themePreset: "CUSTOM_CSS"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidColor_ShouldBeRejected()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-invalid-color@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Invalid Color Cafe", "branding-invalid-color-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest(primaryColor: "red"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ArbitraryCssHtmlOrJavaScript_ShouldNotBeStored()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "branding-unsafe-content@example.com");
        var cafe = await SeedCafeAsync(factory, "Branding Unsafe Cafe", "branding-unsafe-cafe");
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PutAsJsonAsync(
            $"/CafeBranding/UpdateCafeBranding/{cafe.Id}",
            ValidRequest(welcomeTitle: "<script>alert(1)</script>"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoThemeStoredAsync(factory, cafe.Id);
    }

    private static UpdateCafeBrandingRequest ValidRequest(
        string primaryColor = "#1A1A1A",
        string themePreset = CafeThemeConstants.ClassicThemePreset,
        string welcomeTitle = "Welcome")
    {
        return new UpdateCafeBrandingRequest
        {
            LogoImageUrl = "https://cdn.example.com/logo.png",
            CoverImageUrl = "https://cdn.example.com/cover.png",
            PrimaryColor = primaryColor,
            SecondaryColor = "#F5F5F5",
            AccentColor = "#D97706",
            BackgroundColor = "#FFFFFF",
            TextColor = "#111111",
            WelcomeTitle = welcomeTitle,
            WelcomeDescription = "Fresh menu selections",
            FontPreset = CafeThemeConstants.SystemFontPreset,
            ThemePreset = themePreset,
            IsPublished = true
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

    private static async Task<AppUserEntity> SeedUserAsync(
        CustomWebApplicationFactory factory,
        string email,
        string? platformRoleCode = null)
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

        if (platformRoleCode is not null)
        {
            var role = dbContext.Roles.Single(existingRole => existingRole.Code == platformRoleCode);
            user.Roles.Add(role);
        }

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

    private static async Task AssertNoThemeStoredAsync(CustomWebApplicationFactory factory, long cafeId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        Assert.False(dbContext.CafeThemes.Any(theme => theme.CafeId == cafeId));
        await Task.CompletedTask;
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
