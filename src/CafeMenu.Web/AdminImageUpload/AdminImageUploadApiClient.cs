using System.Net;
using CafeMenu.Web.AdminAuth;

namespace CafeMenu.Web.AdminImageUpload;

public sealed class AdminImageUploadApiClient : IAdminImageUploadApiClient
{
    private readonly HttpClient _httpClient;

    public AdminImageUploadApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
    }

    public Task<AdminImageUploadStatus> UploadCafeLogoAsync(
        long cafeId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return UploadAsync($"CafeBranding/UploadLogoImage/{cafeId}", file, cancellationToken);
    }

    public Task<AdminImageUploadStatus> UploadCafeCoverAsync(
        long cafeId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return UploadAsync($"CafeBranding/UploadCoverImage/{cafeId}", file, cancellationToken);
    }

    public Task<AdminImageUploadStatus> RemoveCafeLogoAsync(long cafeId, CancellationToken cancellationToken)
    {
        return PostEmptyAsync($"CafeBranding/RemoveLogoImage/{cafeId}", cancellationToken);
    }

    public Task<AdminImageUploadStatus> RemoveCafeCoverAsync(long cafeId, CancellationToken cancellationToken)
    {
        return PostEmptyAsync($"CafeBranding/RemoveCoverImage/{cafeId}", cancellationToken);
    }

    public Task<AdminImageUploadStatus> UploadCategoryImageAsync(
        long categoryId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return UploadAsync($"Category/UploadCategoryImage/{categoryId}", file, cancellationToken);
    }

    public Task<AdminImageUploadStatus> RemoveCategoryImageAsync(long categoryId, CancellationToken cancellationToken)
    {
        return PostEmptyAsync($"Category/RemoveCategoryImage/{categoryId}", cancellationToken);
    }

    public Task<AdminImageUploadStatus> UploadProductImageAsync(
        long productId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        return UploadAsync($"Product/UploadProductImage/{productId}", file, cancellationToken);
    }

    public Task<AdminImageUploadStatus> RemoveProductImageAsync(long productId, CancellationToken cancellationToken)
    {
        return PostEmptyAsync($"Product/RemoveProductImage/{productId}", cancellationToken);
    }

    private async Task<AdminImageUploadStatus> UploadAsync(
        string requestUri,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            }

            content.Add(fileContent, "File", file.FileName);

            using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
            return ToStatus(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminImageUploadStatus.Failure;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminImageUploadStatus.Failure;
        }
    }

    private async Task<AdminImageUploadStatus> PostEmptyAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsync(requestUri, content: null, cancellationToken);
            return ToStatus(response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminImageUploadStatus.Failure;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminImageUploadStatus.Failure;
        }
    }

    private static AdminImageUploadStatus ToStatus(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.BadRequest)
        {
            return AdminImageUploadStatus.ValidationError;
        }

        return statusCode is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices
            ? AdminImageUploadStatus.Success
            : AdminImageUploadStatus.Failure;
    }
}
