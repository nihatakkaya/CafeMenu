using CafeMenu.Api.Common;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Services;
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
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICafeRepository, CafeRepository>();
        services.AddScoped<ICafeMembershipRepository, CafeMembershipRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<AppUserMapper>();
        services.AddScoped<CafeMapper>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<ICafeService, CafeService>();

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
