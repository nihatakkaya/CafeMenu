using System.Net;
using System.Text.Json;
namespace CafeMenu.Tests;

public sealed class SystemHealthEndpointTests
{
    [Fact]
    public async Task Health_ShouldReturnSuccessfulApiResponse()
    {
        await using var factory = new CustomWebApplicationFactory();

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/System/Health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("Application is healthy.", root.GetProperty("message").GetString());
        Assert.Equal("Healthy", root.GetProperty("data").GetProperty("status").GetString());
    }
}
