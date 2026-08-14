using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminCafe;

public sealed class AdminCafeApiClient : IAdminCafeApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminCafeApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public async Task<AdminCafeListResult> GetMyCafesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("Cafe/GetMyCafes", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AdminCafeListResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<IReadOnlyCollection<AdminCafeResponse>>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminCafeListResult.Success(apiResponse.Data)
                : AdminCafeListResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminCafeListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCafeListResult.Failure();
        }
    }

    public async Task<AdminCafeDashboardStatsResult> GetCafeDashboardStatsAsync(
        long cafeId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"Cafe/GetCafeDashboardStats/{cafeId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AdminCafeDashboardStatsResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminCafeDashboardStatsResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminCafeDashboardStatsResult.Success(apiResponse.Data)
                : AdminCafeDashboardStatsResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminCafeDashboardStatsResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCafeDashboardStatsResult.Failure();
        }
    }
}
