namespace CafeMenu.Api.Exceptions;

public sealed class UnauthorizedApplicationException : ApplicationExceptionBase
{
    public UnauthorizedApplicationException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status401Unauthorized, errorCode)
    {
    }
}
