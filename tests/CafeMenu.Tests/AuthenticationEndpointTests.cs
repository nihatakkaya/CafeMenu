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

public sealed class AuthenticationEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task Register_ShouldNotBeAvailableForPublicSelfRegistration()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/Authentication/Register",
            new
            {
                Email = "owner@example.com",
                FullName = "Test User",
                Password = ValidPassword,
                Roles = new[] { ApplicationRoles.PlatformAdmin, ApplicationRoles.CafeOwner }
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        Assert.Empty(dbContext.AppUsers);
    }

    [Fact]
    public async Task Login_ShouldReturnTokensForExistingUser()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedUserAsync(factory, "login@example.com");
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/Authentication/Login",
            new LoginRequest
            {
                Email = "LOGIN@example.com",
                Password = ValidPassword
            });
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("data").GetProperty("refreshToken").GetString()));
        Assert.Equal("login@example.com", json.RootElement.GetProperty("data").GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public void AppUserModel_ShouldKeepEmailUnique()
    {
        using var factory = new CustomWebApplicationFactory();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var entityType = dbContext.Model.FindEntityType(typeof(AppUserEntity));
        var emailIndex = entityType?.GetIndexes()
            .SingleOrDefault(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(AppUserEntity.Email));

        Assert.NotNull(emailIndex);
        Assert.True(emailIndex.IsUnique);
    }

    [Fact]
    public async Task Login_ShouldRejectWrongPassword()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedUserAsync(factory, "wrong-password@example.com");
        using var client = factory.CreateClient();

        using var loginResponse = await client.PostAsJsonAsync(
            "/Authentication/Login",
            new LoginRequest
            {
                Email = "wrong-password@example.com",
                Password = "wrong-password"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateRefreshTokenAndRejectReusedToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedUserAsync(factory, "refresh@example.com");
        using var client = factory.CreateClient();

        var originalRefreshToken = await LoginAndGetRefreshTokenAsync(client, "refresh@example.com");

        using var refreshResponse = await client.PostAsJsonAsync(
            "/Authentication/RefreshToken",
            new RefreshTokenRequest { RefreshToken = originalRefreshToken });
        var refreshJson = await ParseAsync(refreshResponse);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotatedRefreshToken = refreshJson.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(rotatedRefreshToken));
        Assert.NotEqual(originalRefreshToken, rotatedRefreshToken);

        using var reusedResponse = await client.PostAsJsonAsync(
            "/Authentication/RefreshToken",
            new RefreshTokenRequest { RefreshToken = originalRefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, reusedResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedUserAsync(factory, "logout@example.com");
        using var client = factory.CreateClient();

        var refreshToken = await LoginAndGetRefreshTokenAsync(client, "logout@example.com");

        using var logoutResponse = await client.PostAsJsonAsync(
            "/Authentication/Logout",
            new LogoutRequest { RefreshToken = refreshToken });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        using var refreshResponse = await client.PostAsJsonAsync(
            "/Authentication/RefreshToken",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ShouldRejectExpiredToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedUserAsync(factory, "expired@example.com");
        using var client = factory.CreateClient();

        var refreshToken = await LoginAndGetRefreshTokenAsync(client, "expired@example.com");
        var refreshTokenHash = RefreshTokenGenerator.Hash(refreshToken);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
            var storedToken = dbContext.RefreshTokens.Single(token => token.TokenHash == refreshTokenHash);
            storedToken.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        using var refreshResponse = await client.PostAsJsonAsync(
            "/Authentication/RefreshToken",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldRequireAuthorization()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/Authentication/GetCurrentUser");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUserWhenAuthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedUserAsync(factory, "current@example.com");
        using var client = factory.CreateClient();

        var accessToken = await LoginAndGetAccessTokenAsync(client, "current@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.GetAsync("/Authentication/GetCurrentUser");
        var json = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("current@example.com", json.RootElement.GetProperty("data").GetProperty("email").GetString());
    }

    private static async Task SeedUserAsync(CustomWebApplicationFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var utcNow = DateTimeOffset.UtcNow;

        dbContext.AppUsers.Add(new AppUserEntity
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = "Test User",
            PasswordHash = passwordHasher.HashPassword(ValidPassword),
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<string> LoginAndGetRefreshTokenAsync(HttpClient client, string email)
    {
        using var response = await LoginAsync(client, email);
        var json = await ParseAsync(response);
        return json.RootElement.GetProperty("data").GetProperty("refreshToken").GetString()!;
    }

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email)
    {
        using var response = await LoginAsync(client, email);
        var json = await ParseAsync(response);
        return json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email)
    {
        return client.PostAsJsonAsync(
            "/Authentication/Login",
            new LoginRequest
            {
                Email = email,
                Password = ValidPassword
            });
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
