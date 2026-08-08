namespace CafeMenu.Api.Exceptions;

public sealed class ConflictApplicationException : ApplicationExceptionBase
{
    public ConflictApplicationException(string message)
        : base(message, StatusCodes.Status409Conflict)
    {
    }
}
