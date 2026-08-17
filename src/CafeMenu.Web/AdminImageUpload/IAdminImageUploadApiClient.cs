namespace CafeMenu.Web.AdminImageUpload;

public interface IAdminImageUploadApiClient
{
    Task<AdminImageUploadStatus> UploadCafeLogoAsync(long cafeId, IFormFile file, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> UploadCafeCoverAsync(long cafeId, IFormFile file, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> RemoveCafeLogoAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> RemoveCafeCoverAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> UploadCategoryImageAsync(long categoryId, IFormFile file, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> RemoveCategoryImageAsync(long categoryId, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> UploadProductImageAsync(long productId, IFormFile file, CancellationToken cancellationToken);

    Task<AdminImageUploadStatus> RemoveProductImageAsync(long productId, CancellationToken cancellationToken);
}
