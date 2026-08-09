namespace CafeMenu.Api.Exceptions;

public sealed class ForbiddenApplicationException : ApplicationExceptionBase
{
    public ForbiddenApplicationException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status403Forbidden, errorCode)
    {
    }
}
