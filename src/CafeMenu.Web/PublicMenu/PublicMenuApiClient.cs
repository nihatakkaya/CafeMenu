using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CafeMenu.Web.PublicMenu;

public sealed class PublicMenuApiClient : IPublicMenuApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public PublicMenuApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        try
        {
            using var response = await _httpClient.GetAsync(
                $"PublicMenu/GetMenu/{Uri.EscapeDataString(normalizedSlug)}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return PublicMenuRequestResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                return PublicMenuRequestResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PublicMenuResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? PublicMenuRequestResult.Success(apiResponse.Data)
                : PublicMenuRequestResult.Failure();
        }
        catch (HttpRequestException)
        {
            return PublicMenuRequestResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PublicMenuRequestResult.Failure();
        }
        catch (JsonException)
        {
            return PublicMenuRequestResult.Failure();
        }
    }

    public async Task<PublicProductDetailRequestResult> GetProductDetailAsync(
        string slug,
        long productId,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        try
        {
            using var response = await _httpClient.GetAsync(
                $"PublicMenu/GetProductDetail/{Uri.EscapeDataString(normalizedSlug)}/{productId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return PublicProductDetailRequestResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                return PublicProductDetailRequestResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PublicProductDetailResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? PublicProductDetailRequestResult.Success(apiResponse.Data)
                : PublicProductDetailRequestResult.Failure();
        }
        catch (HttpRequestException)
        {
            return PublicProductDetailRequestResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PublicProductDetailRequestResult.Failure();
        }
        catch (JsonException)
        {
            return PublicProductDetailRequestResult.Failure();
        }
    }
}
