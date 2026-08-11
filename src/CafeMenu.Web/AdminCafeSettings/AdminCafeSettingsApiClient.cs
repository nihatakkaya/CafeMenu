using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminCafeSettings;

public sealed class AdminCafeSettingsApiClient : IAdminCafeSettingsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminCafeSettingsApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public async Task<AdminCafeSettingsRequestResult> GetCafeSettingsAsync(
        long cafeId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"Cafe/GetCafeById/{cafeId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AdminCafeSettingsRequestResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminCafeSettingsResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminCafeSettingsRequestResult.Success(apiResponse.Data)
                : AdminCafeSettingsRequestResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminCafeSettingsRequestResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCafeSettingsRequestResult.Failure();
        }
    }

    public async Task<AdminCafeSettingsRequestResult> UpdateCafeSettingsAsync(
        long cafeId,
        AdminUpdateCafeSettingsRequest request,
        CancellationToken cancellationToken)
    {
        return await SendMutationAsync(
            $"Cafe/UpdateCafe/{cafeId}",
            request,
            cancellationToken);
    }

    public async Task<AdminCafeSettingsRequestResult> ChangeCafePublicationAsync(
        long cafeId,
        AdminChangeCafePublicationRequest request,
        CancellationToken cancellationToken)
    {
        return await SendMutationAsync(
            $"Cafe/ChangeCafePublication/{cafeId}",
            request,
            cancellationToken);
    }

    private async Task<AdminCafeSettingsRequestResult> SendMutationAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PutAsJsonAsync(
                requestUri,
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return AdminCafeSettingsRequestResult.ValidationError();
            }

            if (!response.IsSuccessStatusCode)
            {
                return AdminCafeSettingsRequestResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminCafeSettingsResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminCafeSettingsRequestResult.Success(apiResponse.Data)
                : AdminCafeSettingsRequestResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminCafeSettingsRequestResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCafeSettingsRequestResult.Failure();
        }
    }
}
