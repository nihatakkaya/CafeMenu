namespace CafeMenu.Api.Exceptions;

public sealed class UnauthorizedApplicationException : ApplicationExceptionBase
{
    public UnauthorizedApplicationException(string message)
        : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}
