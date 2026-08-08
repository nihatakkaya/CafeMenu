using CafeMenu.Api.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception is ApplicationExceptionBase applicationException
            ? applicationException.StatusCode
            : StatusCodes.Status500InternalServerError;

        if (exception is ApplicationExceptionBase)
        {
            _logger.LogWarning("Handled application exception for request {TraceId}: {Message}", httpContext.TraceIdentifier, exception.Message);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred while processing request {TraceId}", httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;

        var response = ApiResponse<ProblemDetails>.FailureResponse(
            exception is ApplicationExceptionBase ? exception.Message : "An unexpected error occurred.",
            new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = exception is ApplicationExceptionBase ? exception.Message : "The request could not be completed.",
                Instance = httpContext.Request.Path
            });

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Internal Server Error"
        };
    }
}
