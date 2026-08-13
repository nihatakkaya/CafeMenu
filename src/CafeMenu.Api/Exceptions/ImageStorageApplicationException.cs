namespace CafeMenu.Api.Exceptions;

public sealed class ImageStorageApplicationException : ApplicationExceptionBase
{
    public ImageStorageApplicationException(string message, string? errorCode, Exception innerException)
        : base(message, StatusCodes.Status500InternalServerError, errorCode, innerException)
    {
    }
}
