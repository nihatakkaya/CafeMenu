using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminPlatform;

public sealed class AdminPlatformApiClient : IAdminPlatformApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminPlatformApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public async Task<AdminPlatformCafeListResult> GetCafesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("Cafe/GetCafes", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminPlatformCafeListResult.Failure(GetFailureStatus(response.StatusCode));
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<IReadOnlyCollection<AdminPlatformCafeResponse>>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminPlatformCafeListResult.Success(apiResponse.Data)
                : AdminPlatformCafeListResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminPlatformCafeListResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformCafeListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformCafeListResult.Failure();
        }
    }

    public async Task<AdminPlatformDashboardStatsResult> GetPlatformDashboardStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("Cafe/GetPlatformDashboardStats", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminPlatformDashboardStatsResult.Failure(GetFailureStatus(response.StatusCode));
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminPlatformDashboardStatsResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminPlatformDashboardStatsResult.Success(apiResponse.Data)
                : AdminPlatformDashboardStatsResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminPlatformDashboardStatsResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformDashboardStatsResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformDashboardStatsResult.Failure();
        }
    }

    public Task<AdminPlatformCafeMutationResult> CreateCafeAsync(
        AdminPlatformCreateCafeRequest request,
        CancellationToken cancellationToken)
    {
        return SendCafeMutationAsync(
            "Cafe/CreateCafe",
            HttpMethod.Post,
            request,
            cancellationToken);
    }

    public Task<AdminPlatformCafeMutationResult> ActivateCafeAsync(long cafeId, CancellationToken cancellationToken)
    {
        return SendCafeMutationAsync<object>(
            $"Cafe/ActivateCafe/{cafeId}",
            HttpMethod.Put,
            request: null,
            cancellationToken);
    }

    public Task<AdminPlatformCafeMutationResult> DeactivateCafeAsync(long cafeId, CancellationToken cancellationToken)
    {
        return SendCafeMutationAsync<object>(
            $"Cafe/DeactivateCafe/{cafeId}",
            HttpMethod.Put,
            request: null,
            cancellationToken);
    }

    public async Task<AdminPlatformMemberListResult> GetCafeMembersAsync(
        long cafeId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"Cafe/GetCafeMembers/{cafeId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminPlatformMemberListResult.Failure(GetFailureStatus(response.StatusCode));
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<IReadOnlyCollection<AdminPlatformCafeMemberResponse>>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminPlatformMemberListResult.Success(apiResponse.Data)
                : AdminPlatformMemberListResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminPlatformMemberListResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformMemberListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformMemberListResult.Failure();
        }
    }

    public Task<AdminPlatformUserSetupResult> CreateUserSetupAsync(
        AdminPlatformCreateUserSetupRequest request,
        CancellationToken cancellationToken)
    {
        return SendUserSetupMutationAsync(
            "PlatformUser/CreateUserSetup",
            HttpMethod.Post,
            request,
            cancellationToken);
    }

    public Task<AdminPlatformUserSetupResult> ReissueUserSetupAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        return SendUserSetupMutationAsync<object>(
            $"PlatformUser/ReissueUserSetup/{userId}",
            HttpMethod.Post,
            request: null,
            cancellationToken);
    }

    public async Task<AdminPlatformUserSearchResult> SearchUsersAsync(
        AdminPlatformUserSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = $"PlatformUser/SearchUsers?query={Uri.EscapeDataString(request.Query)}&pageSize={request.PageSize}";
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminPlatformUserSearchResult.Failure(GetFailureStatus(response.StatusCode));
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<IReadOnlyCollection<AdminPlatformUserSearchResponse>>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminPlatformUserSearchResult.Success(apiResponse.Data)
                : AdminPlatformUserSearchResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminPlatformUserSearchResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformUserSearchResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformUserSearchResult.Failure();
        }
    }

    public Task<AdminPlatformMembershipMutationResult> AssignCafeOwnerAsync(
        AdminPlatformAssignCafeMemberRequest request,
        CancellationToken cancellationToken)
    {
        return SendMembershipMutationAsync("Cafe/AssignCafeOwner", request, cancellationToken);
    }

    public Task<AdminPlatformMembershipMutationResult> AssignCafeManagerAsync(
        AdminPlatformAssignCafeMemberRequest request,
        CancellationToken cancellationToken)
    {
        return SendMembershipMutationAsync("Cafe/AssignCafeManager", request, cancellationToken);
    }

    public async Task<AdminPlatformMembershipMutationResult> DeactivateCafeMembershipAsync(
        long membershipId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsync(
                $"Cafe/DeactivateCafeMembership/{membershipId}",
                content: null,
                cancellationToken);

            return await ReadMembershipMutationResultAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return AdminPlatformMembershipMutationResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformMembershipMutationResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformMembershipMutationResult.Failure();
        }
    }

    private async Task<AdminPlatformCafeMutationResult> SendCafeMutationAsync<TRequest>(
        string requestUri,
        HttpMethod httpMethod,
        TRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(httpMethod, requestUri);
            if (request is not null)
            {
                httpRequest.Content = JsonContent.Create(request, options: JsonOptions);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminPlatformCafeMutationResult.Failure(GetFailureStatus(response.StatusCode));
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminPlatformCafeResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminPlatformCafeMutationResult.Success(apiResponse.Data)
                : AdminPlatformCafeMutationResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminPlatformCafeMutationResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformCafeMutationResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformCafeMutationResult.Failure();
        }
    }

    private async Task<AdminPlatformUserSetupResult> SendUserSetupMutationAsync<TRequest>(
        string requestUri,
        HttpMethod httpMethod,
        TRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(httpMethod, requestUri);
            if (request is not null)
            {
                httpRequest.Content = JsonContent.Create(request, options: JsonOptions);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AdminPlatformUserSetupResult.Failure(GetFailureStatus(response.StatusCode));
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminPlatformUserSetupResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminPlatformUserSetupResult.Success(apiResponse.Data)
                : AdminPlatformUserSetupResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminPlatformUserSetupResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformUserSetupResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformUserSetupResult.Failure();
        }
    }

    private async Task<AdminPlatformMembershipMutationResult> SendMembershipMutationAsync(
        string requestUri,
        AdminPlatformAssignCafeMemberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                JsonOptions,
                cancellationToken);

            return await ReadMembershipMutationResultAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return AdminPlatformMembershipMutationResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminPlatformMembershipMutationResult.Failure();
        }
        catch (JsonException)
        {
            return AdminPlatformMembershipMutationResult.Failure();
        }
    }

    private static async Task<AdminPlatformMembershipMutationResult> ReadMembershipMutationResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return AdminPlatformMembershipMutationResult.Failure(GetFailureStatus(response.StatusCode));
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminPlatformMembershipResponse>>(
            JsonOptions,
            cancellationToken);

        return apiResponse is { Success: true, Data: not null }
            ? AdminPlatformMembershipMutationResult.Success(apiResponse.Data)
            : AdminPlatformMembershipMutationResult.Failure();
    }

    private static AdminPlatformRequestStatus GetFailureStatus(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => AdminPlatformRequestStatus.ValidationError,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AdminPlatformRequestStatus.UnauthorizedOrForbidden,
            HttpStatusCode.NotFound => AdminPlatformRequestStatus.NotFound,
            HttpStatusCode.Conflict => AdminPlatformRequestStatus.Conflict,
            _ => AdminPlatformRequestStatus.Failure
        };
    }
}
