namespace CafeMenu.Api.Storage;

public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(ImageUploadInput input, CancellationToken cancellationToken);
}
