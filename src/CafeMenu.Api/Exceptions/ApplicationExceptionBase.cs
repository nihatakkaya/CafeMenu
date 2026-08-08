namespace CafeMenu.Api.Exceptions;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
