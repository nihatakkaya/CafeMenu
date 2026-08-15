using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AdminAuth;

public static class AdminAuthServiceCollectionExtensions
{
    public const string MemoryStoreProductionGuardMessage =
        "Persistent or distributed IAdminSessionTokenStore is required outside Development. MemoryAdminSessionTokenStore is process-local and must not be used in production-like environments.";

    public static IServiceCollection AddAdminAuthenticationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<AdminApiOptions>()
            .Bind(configuration.GetSection("AdminApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminApiOptions>, AdminApiOptionsValidator>();

        services.AddOptions<AdminSessionOptions>()
            .Bind(configuration.GetSection(AdminSessionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminSessionOptions>, AdminSessionOptionsValidator>();

        services.AddHttpContextAccessor();
        services.AddAntiforgery();
        services.AddCascadingAuthenticationState();
        services.AddSingleton(TimeProvider.System);
        AddAdminSessionTokenStore(services, configuration);
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<AdminCookieAuthenticationEvents>();
        services.AddTransient<AdminApiAuthenticationHandler>();

        services
            .AddAuthentication(AdminAuthenticationConstants.CookieScheme)
            .AddCookie(AdminAuthenticationConstants.CookieScheme, options =>
            {
                options.Cookie.Name = "CafeMenu.Admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.EventsType = typeof(AdminCookieAuthenticationEvents);
            });

        services.AddAuthorization();

        services.AddHttpClient<IAdminAuthApiClient, AdminAuthApiClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AdminApiOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        });

        services.AddHttpClient(AdminAuthenticationConstants.AdminApiClientName, (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AdminApiOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .AddHttpMessageHandler<AdminApiAuthenticationHandler>();

        return services;
    }

    private static void AddAdminSessionTokenStore(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration =
                configuration.GetValue<string>($"{AdminSessionOptions.SectionName}:RedisConnectionString")
                ?? "localhost:6379";
        });

        services.AddSingleton<IAdminSessionTokenStore>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AdminSessionOptions>>().Value;

            return AdminSessionProvider.IsRedis(options.Provider)
                ? ActivatorUtilities.CreateInstance<RedisAdminSessionTokenStore>(serviceProvider)
                : ActivatorUtilities.CreateInstance<MemoryAdminSessionTokenStore>(serviceProvider);
        });
    }
}
