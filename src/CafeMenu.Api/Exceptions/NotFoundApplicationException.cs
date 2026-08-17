namespace CafeMenu.Api.Exceptions;

public sealed class NotFoundApplicationException : ApplicationExceptionBase
{
    public NotFoundApplicationException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status404NotFound, errorCode)
    {
    }
}
