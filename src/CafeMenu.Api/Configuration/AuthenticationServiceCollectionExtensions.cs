using System.Text;
using CafeMenu.Api.Common;
using CafeMenu.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CafeMenu.Api.Configuration;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuthentication");

                        logger.LogWarning(context.Exception, "JWT authentication failed.");

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            ApiResponse<object>.FailureResponse("Unauthorized."));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(
                            ApiResponse<object>.FailureResponse("Forbidden."));
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                ApplicationPolicies.PlatformAdministration,
                policy => policy.RequireRole(ApplicationRoles.PlatformAdmin));
        });

        return services;
    }
}
