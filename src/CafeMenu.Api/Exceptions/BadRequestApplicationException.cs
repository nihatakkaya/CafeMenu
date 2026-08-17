namespace CafeMenu.Api.Exceptions;

public sealed class BadRequestApplicationException : ApplicationExceptionBase
{
    public BadRequestApplicationException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status400BadRequest, errorCode)
    {
    }

    public BadRequestApplicationException(string message, string? errorCode, Exception innerException)
        : base(message, StatusCodes.Status400BadRequest, errorCode, innerException)
    {
    }
}
