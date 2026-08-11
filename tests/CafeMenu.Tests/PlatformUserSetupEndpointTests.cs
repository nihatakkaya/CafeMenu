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

public sealed class PlatformUserSetupEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";
    private const string NewPassword = "NewSecurePassword123!";

    [Fact]
    public async Task PlatformAdmin_ShouldCreatePendingUserSetup()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "setup-admin@example.local", ApplicationRoles.PlatformAdmin);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var response = await CreateUserSetupAsync(client, "Owner@Example.Local", "Cafe Owner");
        var json = await ParseAsync(response);
        var data = json.RootElement.GetProperty("data");
        var setupToken = data.GetProperty("setupToken").GetString();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("owner@example.local", data.GetProperty("email").GetString());
        Assert.False(data.GetProperty("isActive").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(setupToken));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var user = dbContext.AppUsers.Include(existingUser => existingUser.Roles).Single(existingUser => existingUser.Email == "owner@example.local");
        var token = dbContext.UserSetupTokens.Single(existingToken => existingToken.AppUserId == user.Id);

        Assert.False(user.IsActive);
        Assert.Empty(user.Roles);
        Assert.DoesNotContain(ApplicationRoles.CafeOwner, user.Roles.Select(role => role.Code));
        Assert.DoesNotContain(ApplicationRoles.CafeManager, user.Roles.Select(role => role.Code));
        Assert.NotEqual(setupToken, token.TokenHash);
        Assert.Equal(UserSetupTokenGenerator.Hash(setupToken!), token.TokenHash);
        Assert.Null(token.ConsumedAt);
        Assert.True(token.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(token.ExpiresAt <= DateTimeOffset.UtcNow.AddHours(25));
    }

    [Fact]
    public async Task CreateUserSetup_ShouldRequirePlatformAdmin()
    {
        await using var factory = new CustomWebApplicationFactory();
        var normalUser = await SeedUserAsync(factory, "normal-user@example.local");
        using var anonymousClient = factory.CreateClient();
        using var authenticatedClient = factory.CreateClient();
        await AuthorizeAsync(authenticatedClient, normalUser.Email);

        using var anonymousResponse = await CreateUserSetupAsync(anonymousClient, "anon@example.local", "Anon User");
        using var forbiddenResponse = await CreateUserSetupAsync(authenticatedClient, "forbidden@example.local", "Forbidden User");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task CreateUserSetup_ShouldRejectDuplicateEmail()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "duplicate-admin@example.local", ApplicationRoles.PlatformAdmin);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var firstResponse = await CreateUserSetupAsync(client, "duplicate@example.local", "Duplicate User");
        using var secondResponse = await CreateUserSetupAsync(client, "DUPLICATE@example.local", "Duplicate User");

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CompleteUserSetup_ShouldActivateUserAndAllowLogin()
    {
        await using var factory = new CustomWebApplicationFactory();
        var (userId, setupToken) = await CreatePendingUserAsync(factory, "complete@example.local");
        using var client = factory.CreateClient();

        using var completeResponse = await CompleteSetupAsync(client, setupToken, NewPassword, NewPassword);
        var completeJson = await ParseAsync(completeResponse);

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.Equal(userId, completeJson.RootElement.GetProperty("data").GetProperty("id").GetInt64());
        Assert.True(completeJson.RootElement.GetProperty("data").GetProperty("isActive").GetBoolean());

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var user = dbContext.AppUsers.Single(existingUser => existingUser.Id == userId);
            var token = dbContext.UserSetupTokens.Single(existingToken => existingToken.AppUserId == userId);

            Assert.True(user.IsActive);
            Assert.True(passwordHasher.VerifyPassword(NewPassword, user.PasswordHash));
            Assert.NotEqual(NewPassword, user.PasswordHash);
            Assert.NotNull(token.ConsumedAt);
        }

        using var loginResponse = await client.PostAsJsonAsync(
            "/Authentication/Login",
            new LoginRequest { Email = "complete@example.local", Password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task CompleteUserSetup_ShouldRejectConsumedExpiredInvalidAndDeletedUserTokens()
    {
        await using var consumedFactory = new CustomWebApplicationFactory();
        var (_, consumedToken) = await CreatePendingUserAsync(consumedFactory, "consumed@example.local");
        using (var client = consumedFactory.CreateClient())
        {
            using var firstResponse = await CompleteSetupAsync(client, consumedToken, NewPassword, NewPassword);
            using var secondResponse = await CompleteSetupAsync(client, consumedToken, NewPassword, NewPassword);

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
        }

        await using var expiredFactory = new CustomWebApplicationFactory();
        var (expiredUserId, expiredToken) = await CreatePendingUserAsync(expiredFactory, "expired-setup@example.local");
        await MutateUserSetupAsync(expiredFactory, expiredUserId, setupToken =>
        {
            setupToken.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            setupToken.UpdatedAt = DateTimeOffset.UtcNow;
        });
        using (var client = expiredFactory.CreateClient())
        {
            using var response = await CompleteSetupAsync(client, expiredToken, NewPassword, NewPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        await using var invalidFactory = new CustomWebApplicationFactory();
        using (var client = invalidFactory.CreateClient())
        {
            var invalidToken = UserSetupTokenGenerator.Generate();
            using var response = await CompleteSetupAsync(client, invalidToken, NewPassword, NewPassword);
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain(invalidToken, responseBody, StringComparison.Ordinal);
        }

        await using var deletedFactory = new CustomWebApplicationFactory();
        var (deletedUserId, deletedToken) = await CreatePendingUserAsync(deletedFactory, "deleted-setup@example.local");
        await MutateUserSetupAsync(deletedFactory, deletedUserId, (_, user) =>
        {
            user.IsDeleted = true;
            user.DeletedAt = DateTimeOffset.UtcNow;
        });
        using (var client = deletedFactory.CreateClient())
        {
            using var response = await CompleteSetupAsync(client, deletedToken, NewPassword, NewPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task CompleteUserSetup_ShouldApplyPasswordPolicyAndConfirmation()
    {
        await using var factory = new CustomWebApplicationFactory();
        var (_, setupToken) = await CreatePendingUserAsync(factory, "policy@example.local");
        using var client = factory.CreateClient();

        using var weakPasswordResponse = await CompleteSetupAsync(client, setupToken, "weak", "weak");
        using var mismatchResponse = await CompleteSetupAsync(client, setupToken, NewPassword, "DifferentSecurePassword123!");

        Assert.Equal(HttpStatusCode.BadRequest, weakPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mismatchResponse.StatusCode);
    }

    [Fact]
    public async Task ReissueUserSetup_ShouldCreateNewTokenAndInvalidateOldToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "reissue-admin@example.local", ApplicationRoles.PlatformAdmin);
        var (userId, oldToken) = await CreatePendingUserAsync(factory, "reissue@example.local");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var reissueResponse = await client.PostAsync($"/PlatformUser/ReissueUserSetup/{userId}", null);
        var reissueJson = await ParseAsync(reissueResponse);
        var newToken = reissueJson.RootElement.GetProperty("data").GetProperty("setupToken").GetString()!;

        Assert.Equal(HttpStatusCode.OK, reissueResponse.StatusCode);
        Assert.NotEqual(oldToken, newToken);

        client.DefaultRequestHeaders.Authorization = null;
        using var oldTokenResponse = await CompleteSetupAsync(client, oldToken, NewPassword, NewPassword);
        using var newTokenResponse = await CompleteSetupAsync(client, newToken, NewPassword, NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newTokenResponse.StatusCode);
    }

    [Fact]
    public async Task ReissueUserSetup_ShouldRejectActiveUser()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "active-reissue-admin@example.local", ApplicationRoles.PlatformAdmin);
        var (userId, setupToken) = await CreatePendingUserAsync(factory, "active-reissue@example.local");
        using var client = factory.CreateClient();
        using var completeResponse = await CompleteSetupAsync(client, setupToken, NewPassword, NewPassword);
        await AuthorizeAsync(client, admin.Email);

        using var reissueResponse = await client.PostAsync($"/PlatformUser/ReissueUserSetup/{userId}", null);

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, reissueResponse.StatusCode);
    }

    [Fact]
    public async Task UserSetupResponses_ShouldNotExposePasswordHashOrTokenHash()
    {
        await using var factory = new CustomWebApplicationFactory();
        var admin = await SeedUserAsync(factory, "exposure-admin@example.local", ApplicationRoles.PlatformAdmin);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var createResponse = await CreateUserSetupAsync(client, "exposure@example.local", "Exposure User");
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var setupToken = JsonDocument.Parse(createContent).RootElement.GetProperty("data").GetProperty("setupToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = null;
        using var completeResponse = await CompleteSetupAsync(client, setupToken, NewPassword, NewPassword);
        var completeContent = await completeResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", createContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", createContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", createContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", completeContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("setupToken", completeContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", completeContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UserSetupTokenModel_ShouldUseUniqueTokenHashAndAppUserForeignKey()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var entityType = dbContext.Model.FindEntityType(typeof(UserSetupTokenEntity));

        var tokenHashIndex = entityType?.GetIndexes().SingleOrDefault(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(UserSetupTokenEntity.TokenHash));
        var appUserForeignKey = entityType?.GetForeignKeys().SingleOrDefault(foreignKey =>
            foreignKey.Properties.Count == 1 &&
            foreignKey.Properties[0].Name == nameof(UserSetupTokenEntity.AppUserId));

        Assert.NotNull(tokenHashIndex);
        Assert.True(tokenHashIndex.IsUnique);
        Assert.NotNull(appUserForeignKey);
        Assert.Equal(DeleteBehavior.Restrict, appUserForeignKey.DeleteBehavior);
    }

    private static async Task<(long UserId, string SetupToken)> CreatePendingUserAsync(
        CustomWebApplicationFactory factory,
        string email)
    {
        var admin = await SeedUserAsync(factory, $"admin-{Guid.NewGuid():N}@example.local", ApplicationRoles.PlatformAdmin);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, admin.Email);

        using var response = await CreateUserSetupAsync(client, email, "Pending User");
        var json = await ParseAsync(response);
        var data = json.RootElement.GetProperty("data");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (data.GetProperty("userId").GetInt64(), data.GetProperty("setupToken").GetString()!);
    }

    private static Task<HttpResponseMessage> CreateUserSetupAsync(HttpClient client, string email, string fullName)
    {
        return client.PostAsJsonAsync(
            "/PlatformUser/CreateUserSetup",
            new CreateUserSetupRequest
            {
                Email = email,
                FullName = fullName
            });
    }

    private static Task<HttpResponseMessage> CompleteSetupAsync(
        HttpClient client,
        string token,
        string password,
        string confirmPassword)
    {
        return client.PostAsJsonAsync(
            "/PlatformUser/CompleteUserSetup",
            new CompleteUserSetupRequest
            {
                Token = token,
                Password = password,
                ConfirmPassword = confirmPassword
            });
    }

    private static async Task MutateUserSetupAsync(
        CustomWebApplicationFactory factory,
        long userId,
        Action<UserSetupTokenEntity> mutate)
    {
        await MutateUserSetupAsync(factory, userId, (setupToken, _) => mutate(setupToken));
    }

    private static async Task MutateUserSetupAsync(
        CustomWebApplicationFactory factory,
        long userId,
        Action<UserSetupTokenEntity, AppUserEntity> mutate)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var user = dbContext.AppUsers.Single(existingUser => existingUser.Id == userId);
        var setupToken = dbContext.UserSetupTokens.Single(existingToken => existingToken.AppUserId == userId);
        mutate(setupToken, user);
        await dbContext.SaveChangesAsync();
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
            FullName = "Test User",
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
