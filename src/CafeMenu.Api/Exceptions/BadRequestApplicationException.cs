namespace CafeMenu.Api.Exceptions;

public sealed class BadRequestApplicationException : ApplicationExceptionBase
{
    public BadRequestApplicationException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status400BadRequest, errorCode)
    {
    }
}
