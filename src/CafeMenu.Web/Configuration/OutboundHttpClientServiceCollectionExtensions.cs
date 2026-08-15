using Microsoft.Extensions.Options;

namespace CafeMenu.Web.Configuration;

public static class OutboundHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddOutboundHttpClientConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<OutboundHttpClientOptions>()
            .Bind(configuration.GetSection(OutboundHttpClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OutboundHttpClientOptions>, OutboundHttpClientOptionsValidator>();

        return services;
    }

    public static IHttpClientBuilder ConfigureOutboundHttpTimeout(this IHttpClientBuilder builder)
    {
        return builder.ConfigureHttpClient((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OutboundHttpClientOptions>>().Value;
            httpClient.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }
}
