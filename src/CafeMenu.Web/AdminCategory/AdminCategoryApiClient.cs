using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminCategory;

public sealed class AdminCategoryApiClient : IAdminCategoryApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminCategoryApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public async Task<AdminCategoryListResult> GetCategoriesAsync(long cafeId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"Category/GetCategories/{cafeId}",
                cancellationToken);

            return await ReadListResultAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return AdminCategoryListResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminCategoryListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCategoryListResult.Failure();
        }
    }

    public Task<AdminCategoryMutationResult> CreateCategoryAsync(
        AdminCreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            "Category/CreateCategory",
            request,
            HttpMethod.Post,
            cancellationToken);
    }

    public Task<AdminCategoryMutationResult> UpdateCategoryAsync(
        long categoryId,
        AdminUpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Category/UpdateCategory/{categoryId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public async Task<AdminCategoryDeleteResult> DeleteCategoryAsync(long categoryId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync(
                $"Category/DeleteCategory/{categoryId}",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? AdminCategoryDeleteResult.Success()
                : AdminCategoryDeleteResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminCategoryDeleteResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminCategoryDeleteResult.Failure();
        }
    }

    public Task<AdminCategoryMutationResult> ChangeCategoryVisibilityAsync(
        long categoryId,
        AdminChangeCategoryVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Category/ChangeCategoryVisibility/{categoryId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public Task<AdminCategoryMutationResult> ChangeCategoryPublicationAsync(
        long categoryId,
        AdminChangeCategoryPublicationRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Category/ChangeCategoryPublication/{categoryId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public async Task<AdminCategoryListResult> ReorderCategoriesAsync(
        AdminReorderCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PutAsJsonAsync(
                "Category/ReorderCategories",
                request,
                JsonOptions,
                cancellationToken);

            return await ReadListResultAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return AdminCategoryListResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminCategoryListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCategoryListResult.Failure();
        }
    }

    private async Task<AdminCategoryMutationResult> SendMutationAsync<TRequest>(
        string requestUri,
        TRequest request,
        HttpMethod httpMethod,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(httpMethod, requestUri)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return AdminCategoryMutationResult.ValidationError();
            }

            if (!response.IsSuccessStatusCode)
            {
                return AdminCategoryMutationResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminCategoryResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminCategoryMutationResult.Success(apiResponse.Data)
                : AdminCategoryMutationResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminCategoryMutationResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminCategoryMutationResult.Failure();
        }
        catch (JsonException)
        {
            return AdminCategoryMutationResult.Failure();
        }
    }

    private static async Task<AdminCategoryListResult> ReadListResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return AdminCategoryListResult.Failure();
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<IReadOnlyCollection<AdminCategoryResponse>>>(
            JsonOptions,
            cancellationToken);

        return apiResponse is { Success: true, Data: not null }
            ? AdminCategoryListResult.Success(apiResponse.Data)
            : AdminCategoryListResult.Failure();
    }
}
