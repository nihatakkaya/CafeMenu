using System.Net;
using CafeMenu.Api.Bootstrap;
using CafeMenu.Api.Data;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Tests;

public sealed class PlatformAdminBootstrapTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task Bootstrap_ShouldCreateNewPlatformAdmin()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedPlatformAdminRoleAsync(factory);

        using var scope = factory.Services.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<IPlatformAdminBootstrapService>();

        var result = await bootstrapService.BootstrapAsync(
            new PlatformAdminBootstrapRequest("Admin@Example.Local", ValidPassword),
            CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var user = await dbContext.AppUsers
            .Include(appUser => appUser.Roles)
            .SingleAsync(appUser => appUser.Email == "admin@example.local");

        Assert.Equal(PlatformAdminBootstrapStatus.Created, result.Status);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Platform Admin", user.FullName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Bootstrap_ShouldAssignOnlyPlatformAdminRole()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedPlatformAdminRoleAsync(factory);

        using var scope = factory.Services.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<IPlatformAdminBootstrapService>();

        await bootstrapService.BootstrapAsync(
            new PlatformAdminBootstrapRequest("admin-role@example.local", ValidPassword),
            CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var user = await dbContext.AppUsers
            .Include(appUser => appUser.Roles)
            .SingleAsync(appUser => appUser.Email == "admin-role@example.local");

        var role = Assert.Single(user.Roles);
        Assert.Equal(ApplicationRoles.PlatformAdmin, role.Code);
    }

    [Fact]
    public async Task Bootstrap_ShouldNotCreateDuplicateUserForDuplicateEmail()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedPlatformAdminRoleAsync(factory);

        using var scope = factory.Services.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<IPlatformAdminBootstrapService>();

        var firstResult = await bootstrapService.BootstrapAsync(
            new PlatformAdminBootstrapRequest("duplicate@example.local", ValidPassword),
            CancellationToken.None);
        var secondResult = await bootstrapService.BootstrapAsync(
            new PlatformAdminBootstrapRequest("DUPLICATE@example.local", "AnotherSecurePassword123!"),
            CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var userCount = await dbContext.AppUsers.CountAsync(appUser => appUser.Email == "duplicate@example.local");

        Assert.Equal(PlatformAdminBootstrapStatus.Created, firstResult.Status);
        Assert.Equal(PlatformAdminBootstrapStatus.AlreadyExists, secondResult.Status);
        Assert.Equal(firstResult.UserId, secondResult.UserId);
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task Bootstrap_ShouldStorePasswordAsBCryptHash()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedPlatformAdminRoleAsync(factory);

        using var scope = factory.Services.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<IPlatformAdminBootstrapService>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await bootstrapService.BootstrapAsync(
            new PlatformAdminBootstrapRequest("hash@example.local", ValidPassword),
            CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var user = await dbContext.AppUsers.SingleAsync(appUser => appUser.Email == "hash@example.local");

        Assert.NotEqual(ValidPassword, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash, StringComparison.Ordinal);
        Assert.True(passwordHasher.VerifyPassword(ValidPassword, user.PasswordHash));
    }

    [Fact]
    public async Task NormalStartup_ShouldStillServeHealthEndpoint()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/System/Health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task SeedPlatformAdminRoleAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();

        if (await dbContext.Roles.AnyAsync(role => role.Code == ApplicationRoles.PlatformAdmin))
        {
            return;
        }

        var utcNow = DateTimeOffset.UtcNow;
        dbContext.Roles.Add(new RoleEntity
        {
            Code = ApplicationRoles.PlatformAdmin,
            Name = "Platform Administrator",
            Description = "Manages platform-level administration.",
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
