using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminProduct;

public sealed class AdminProductApiClient : IAdminProductApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AdminProductApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public async Task<AdminProductListResult> GetProductsAsync(long cafeId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"Product/GetProducts/{cafeId}",
                cancellationToken);

            return await ReadListResultAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return AdminProductListResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminProductListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminProductListResult.Failure();
        }
    }

    public Task<AdminProductMutationResult> CreateProductAsync(
        AdminCreateProductRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            "Product/CreateProduct",
            request,
            HttpMethod.Post,
            cancellationToken);
    }

    public Task<AdminProductMutationResult> UpdateProductAsync(
        long productId,
        AdminUpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Product/UpdateProduct/{productId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public async Task<AdminProductDeleteResult> DeleteProductAsync(long productId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync(
                $"Product/DeleteProduct/{productId}",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? AdminProductDeleteResult.Success()
                : AdminProductDeleteResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminProductDeleteResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminProductDeleteResult.Failure();
        }
    }

    public Task<AdminProductMutationResult> ChangeProductVisibilityAsync(
        long productId,
        AdminChangeProductVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Product/ChangeProductVisibility/{productId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public Task<AdminProductMutationResult> ChangeProductAvailabilityAsync(
        long productId,
        AdminChangeProductAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Product/ChangeProductAvailability/{productId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public Task<AdminProductMutationResult> ChangeProductPublicationAsync(
        long productId,
        AdminChangeProductPublicationRequest request,
        CancellationToken cancellationToken)
    {
        return SendMutationAsync(
            $"Product/ChangeProductPublication/{productId}",
            request,
            HttpMethod.Put,
            cancellationToken);
    }

    public async Task<AdminProductListResult> ReorderProductsAsync(
        AdminReorderProductsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PutAsJsonAsync(
                "Product/ReorderProducts",
                request,
                JsonOptions,
                cancellationToken);

            return await ReadListResultAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return AdminProductListResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminProductListResult.Failure();
        }
        catch (JsonException)
        {
            return AdminProductListResult.Failure();
        }
    }

    private async Task<AdminProductMutationResult> SendMutationAsync<TRequest>(
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
                return AdminProductMutationResult.ValidationError();
            }

            if (!response.IsSuccessStatusCode)
            {
                return AdminProductMutationResult.Failure();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<AdminProductResponse>>(
                JsonOptions,
                cancellationToken);

            return apiResponse is { Success: true, Data: not null }
                ? AdminProductMutationResult.Success(apiResponse.Data)
                : AdminProductMutationResult.Failure();
        }
        catch (HttpRequestException)
        {
            return AdminProductMutationResult.Failure();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminProductMutationResult.Failure();
        }
        catch (JsonException)
        {
            return AdminProductMutationResult.Failure();
        }
    }

    private static async Task<AdminProductListResult> ReadListResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return AdminProductListResult.Failure();
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<AdminApiResponse<IReadOnlyCollection<AdminProductResponse>>>(
            JsonOptions,
            cancellationToken);

        return apiResponse is { Success: true, Data: not null }
            ? AdminProductListResult.Success(apiResponse.Data)
            : AdminProductListResult.Failure();
    }
}
