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
        var applicationException = exception as ApplicationExceptionBase;
        var statusCode = applicationException is not null
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

        var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = exception is ApplicationExceptionBase ? exception.Message : "The request could not be completed.",
                Instance = httpContext.Request.Path
            };

        if (!string.IsNullOrWhiteSpace(applicationException?.ErrorCode))
        {
            problemDetails.Extensions["errorCode"] = applicationException.ErrorCode;
        }

        var response = ApiResponse<ProblemDetails>.FailureResponse(
            exception is ApplicationExceptionBase ? exception.Message : "An unexpected error occurred.",
            problemDetails);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Internal Server Error"
        };
    }
}
