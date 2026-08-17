using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminBranding;

public sealed class AdminBrandingApiClient : IAdminBrandingApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminBrandingApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public async Task<AdminBrandingRequestResult> GetCafeBrandingAsync(
        long cafeId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"CafeBranding/GetCafeBranding/{cafeId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AdminBrandingRequestResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminBrandingResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminBrandingRequestResult.Success(apiResponse.Data)
                : AdminBrandingRequestResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminBrandingRequestResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminBrandingRequestResult.Failure();
        }
        catch (JsonException)
        {
            return AdminBrandingRequestResult.Failure();
        }
    }

    public async Task<AdminBrandingRequestResult> UpdateCafeBrandingAsync(
        long cafeId,
        AdminUpdateCafeBrandingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PutAsJsonAsync(
                $"CafeBranding/UpdateCafeBranding/{cafeId}",
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return AdminBrandingRequestResult.ValidationError();
            }

            if (!response.IsSuccessStatusCode)
            {
                return AdminBrandingRequestResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminBrandingResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminBrandingRequestResult.Success(apiResponse.Data)
                : AdminBrandingRequestResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminBrandingRequestResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminBrandingRequestResult.Failure();
        }
        catch (JsonException)
        {
            return AdminBrandingRequestResult.Failure();
        }
    }
}
