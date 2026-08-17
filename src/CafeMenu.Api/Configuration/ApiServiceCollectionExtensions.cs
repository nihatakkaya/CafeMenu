using CafeMenu.Api.Common;
using CafeMenu.Api.Bootstrap;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Services;
using CafeMenu.Api.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

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
        services.AddScoped<IUserSetupTokenRepository, UserSetupTokenRepository>();
        services.AddScoped<ICafeRepository, CafeRepository>();
        services.AddScoped<ICafeMembershipRepository, CafeMembershipRepository>();
        services.AddScoped<ICafeThemeRepository, CafeThemeRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPublicMenuRepository, PublicMenuRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IImageProcessor, ImageProcessor>();
        services.AddScoped<IImageStorage, LocalImageStorage>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<AppUserMapper>();
        services.AddScoped<PlatformUserMapper>();
        services.AddScoped<CafeMapper>();
        services.AddScoped<CafeThemeMapper>();
        services.AddScoped<CategoryMapper>();
        services.AddScoped<ProductMapper>();
        services.AddScoped<PublicMenuMapper>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IPlatformAdminBootstrapService, PlatformAdminBootstrapService>();
        services.AddScoped<IPlatformUserService, PlatformUserService>();
        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<ICafeService, CafeService>();
        services.AddScoped<ICafeBrandingService, CafeBrandingService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPublicMenuService, PublicMenuService>();
        services.AddSingleton<IConsolePasswordReader, ConsolePasswordReader>();
        services.AddSingleton<PlatformAdminBootstrapRunner>();
        services.AddSingleton<IValidateOptions<ImageStorageOptions>, ImageStorageOptionsValidator>();
        services.AddSingleton<IConfigureOptions<FormOptions>, ImageStorageFormOptionsSetup>();

        services.AddOptions<ImageStorageOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(ImageStorageOptions.SectionName).Bind(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<UserSetupOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(UserSetupOptions.SectionName).Bind(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddApplicationOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token without the Bearer prefix."
            });

            options.DocumentFilter<AuthorizeDocumentFilter>();
        });

        return services;
    }
}
