extern alias CafeMenuWeb;

using System.Net;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;

namespace CafeMenu.Tests;

public sealed class PublicMenuApiClientTests
{
    [Fact]
    public async Task GetMenuAsync_ShouldCallPublicMenuEndpointWithSlug()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var apiClient = new PublicMenuApiClient(httpClient);

        var result = await apiClient.GetMenuAsync("mocca-cafe", CancellationToken.None);

        Assert.Equal(PublicMenuRequestStatus.NotFound, result.Status);
        Assert.Equal("https://api.example.test/PublicMenu/GetMenu/mocca-cafe", handler.RequestUri?.ToString());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(_response);
        }
    }
}
