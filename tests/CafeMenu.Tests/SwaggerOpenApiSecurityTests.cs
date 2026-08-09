using System.Net;
using System.Text.Json;

namespace CafeMenu.Tests;

public sealed class SwaggerOpenApiSecurityTests
{
    private const string BearerSchemeId = "Bearer";

    [Fact]
    public async Task SwaggerDocument_ShouldApplyBearerSecurityOnlyToAuthorizedEndpoints()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        AssertSecuritySchemeIsDefined(root);
        AssertOperationRequiresBearer(root, "/Authentication/GetCurrentUser", "get");
        AssertOperationRequiresBearer(root, "/Cafe/CreateCafe", "post");
        AssertOperationDoesNotRequireBearer(root, "/Authentication/Login", "post");
        AssertOperationDoesNotRequireBearer(root, "/PublicMenu/GetMenu/{slug}", "get");
    }

    private static void AssertSecuritySchemeIsDefined(JsonElement root)
    {
        var securitySchemes = root
            .GetProperty("components")
            .GetProperty("securitySchemes");

        Assert.True(securitySchemes.TryGetProperty(BearerSchemeId, out var bearerScheme));
        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearerScheme.GetProperty("bearerFormat").GetString());
    }

    private static void AssertOperationRequiresBearer(JsonElement root, string path, string method)
    {
        var operation = GetOperation(root, path, method);

        Assert.True(operation.TryGetProperty("security", out var security));
        Assert.Contains(
            security.EnumerateArray(),
            requirement => requirement.TryGetProperty(BearerSchemeId, out var scopes) &&
                scopes.ValueKind == JsonValueKind.Array);
    }

    private static void AssertOperationDoesNotRequireBearer(JsonElement root, string path, string method)
    {
        var operation = GetOperation(root, path, method);

        if (!operation.TryGetProperty("security", out var security))
        {
            return;
        }

        Assert.DoesNotContain(
            security.EnumerateArray(),
            requirement => requirement.TryGetProperty(BearerSchemeId, out _));
    }

    private static JsonElement GetOperation(JsonElement root, string path, string method)
    {
        return root
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(method);
    }
}
