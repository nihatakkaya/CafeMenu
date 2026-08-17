namespace CafeMenu.Api.Exceptions;

public sealed class ConflictApplicationException : ApplicationExceptionBase
{
    public ConflictApplicationException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status409Conflict, errorCode)
    {
    }
}
