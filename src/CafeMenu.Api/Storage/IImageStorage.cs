namespace CafeMenu.Api.Storage;

public interface IImageStorage
{
    Task<StoredImage> StoreAsync(
        ImageUploadInput input,
        ImageStorageFolder folder,
        CancellationToken cancellationToken);

    Task DeleteIfManagedAsync(string? publicUrl, CancellationToken cancellationToken);

    Task<StoredImageFile?> GetAsync(
        ImageStorageFolder folder,
        string fileName,
        CancellationToken cancellationToken);

    bool IsManagedUrl(string? publicUrl);
}
