namespace CafeMenu.Web.AdminImageUpload;

public sealed class AdminImageUploadForm
{
    public IFormFile? File { get; init; }
}

public enum AdminImageUploadStatus
{
    Success,
    ValidationError,
    Failure
}
