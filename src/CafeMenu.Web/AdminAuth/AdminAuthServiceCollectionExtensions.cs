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
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(MemoryStoreProductionGuardMessage);
        }

        services.AddOptions<AdminApiOptions>()
            .Bind(configuration.GetSection("AdminApi"))
            .ValidateDataAnnotations()
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Admin API base URL must be absolute.")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddAntiforgery();
        services.AddCascadingAuthenticationState();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAdminSessionTokenStore, MemoryAdminSessionTokenStore>();
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
}
