using CafeMenu.Api.Common;
using CafeMenu.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(modelState => modelState.Value?.Errors.Count > 0)
                        .ToDictionary(
                            modelState => modelState.Key,
                            modelState => modelState.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

                    var response = ApiResponse<IReadOnlyDictionary<string, string[]>>.FailureResponse(
                        "Validation failed.",
                        errors);

                    return new BadRequestObjectResult(response);
                };
            });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddApplicationOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddSwaggerGen();

        return services;
    }
}
