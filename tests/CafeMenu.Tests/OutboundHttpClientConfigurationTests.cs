extern alias CafeMenuWeb;

using System.Diagnostics;
using System.Net;
using CafeMenuWeb::CafeMenu.Web.AccountSetup;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.Configuration;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class OutboundHttpClientConfigurationTests
{
    [Fact]
    public void Validator_ShouldAllowDefaultTimeoutConfiguration()
    {
        var result = Validate(new OutboundHttpClientOptions());

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(120)]
    public void Validator_ShouldAllowBoundedTimeoutConfiguration(int timeoutSeconds)
    {
        var result = Validate(new OutboundHttpClientOptions
        {
            DefaultTimeoutSeconds = timeoutSeconds
        });

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldRejectTooSmallTimeoutConfiguration(int timeoutSeconds)
    {
        var result = Validate(new OutboundHttpClientOptions
        {
            DefaultTimeoutSeconds = timeoutSeconds
        });

        AssertValidationFailed(result, "DefaultTimeoutSeconds");
    }

    [Fact]
    public void Validator_ShouldRejectTooLargeTimeoutConfiguration()
    {
        var result = Validate(new OutboundHttpClientOptions
        {
            DefaultTimeoutSeconds = 121
        });

        AssertValidationFailed(result, "DefaultTimeoutSeconds");
    }

    [Fact]
    public void StartupValidation_ShouldFailFastForInvalidTimeoutConfiguration()
    {
        using var factory = new OutboundHttpWebApplicationFactory(new Dictionary<string, string?>
        {
            ["HttpClients:DefaultTimeoutSeconds"] = "0"
        });

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("HttpClients:DefaultTimeoutSeconds", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_ShouldApplyConfiguredTimeoutToOutboundHttpClients()
    {
        using var serviceProvider = BuildServiceProvider(timeoutSeconds: 7);
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        using var accountSetupClient = httpClientFactory.CreateClient(AccountSetupConstants.ApiClientName);
        using var adminClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
        using var probeClient = httpClientFactory.CreateClient("ProbeClient");

        Assert.Equal(TimeSpan.FromSeconds(7), accountSetupClient.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(7), adminClient.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(7), probeClient.Timeout);
    }

    [Fact]
    public void Registration_ShouldPreserveConfiguredBaseAddresses()
    {
        using var serviceProvider = BuildServiceProvider(timeoutSeconds: 15);
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        using var accountSetupClient = httpClientFactory.CreateClient(AccountSetupConstants.ApiClientName);
        using var adminClient = httpClientFactory.CreateClient(AdminAuthenticationConstants.AdminApiClientName);
        using var probeClient = httpClientFactory.CreateClient("ProbeClient");

        Assert.Equal(new Uri("https://api.example.test/"), accountSetupClient.BaseAddress);
        Assert.Equal(new Uri("https://api.example.test/"), adminClient.BaseAddress);
        Assert.Equal(new Uri("https://public-api.example.test/"), probeClient.BaseAddress);
    }

    [Fact]
    public async Task PublicMenuApiClient_ShouldPassCallerCancellationTokenToOutboundRequest()
    {
        var handler = new WaitingHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var client = new PublicMenuApiClient(httpClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        var requestTask = client.GetMenuAsync("mocca", cancellationTokenSource.Token);
        await handler.RequestStarted.Task;
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
        Assert.True(handler.ObservedCancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task PublicMenuApiClient_ShouldReturnFailureWhenHttpClientTimeoutCancelsRequest()
    {
        using var httpClient = new HttpClient(new HangingHttpMessageHandler())
        {
            BaseAddress = new Uri("https://api.example.test/"),
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        var client = new PublicMenuApiClient(httpClient);
        var stopwatch = Stopwatch.StartNew();

        var result = await client.GetMenuAsync("mocca", CancellationToken.None);

        stopwatch.Stop();
        Assert.Equal(PublicMenuRequestStatus.Failure, result.Status);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PublicMenuApiClient_ShouldNotSwallowCallerCancellation()
    {
        using var httpClient = new HttpClient(new HangingHttpMessageHandler())
        {
            BaseAddress = new Uri("https://api.example.test/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var client = new PublicMenuApiClient(httpClient);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetMenuAsync("mocca", cancellationTokenSource.Token));
    }

    [Fact]
    public async Task AdminAuthMutationRequest_ShouldNotRetryFailedPost()
    {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var client = new AdminAuthApiClient(httpClient);

        var result = await client.LoginAsync("owner@example.test", "Secret123!", CancellationToken.None);

        Assert.Null(result);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.example.test/Authentication/Login", handler.Requests[0].RequestUri?.ToString());
    }

    private static ServiceProvider BuildServiceProvider(int timeoutSeconds)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HttpClients:DefaultTimeoutSeconds"] = timeoutSeconds.ToString(),
                ["AdminApi:BaseUrl"] = "https://api.example.test/",
                ["PublicApi:BaseUrl"] = "https://public-api.example.test/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddOutboundHttpClientConfiguration(configuration);
        services.AddSingleton<IAdminSessionTokenStore, StubAdminSessionTokenStore>();
        services.AddSingleton<IAdminAuthApiClient>(_ =>
        {
            var client = new HttpClient(new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
            return new AdminAuthApiClient(client);
        });
        services.AddTransient<AdminApiAuthenticationHandler>();
        services.AddOptions<AdminApiOptions>()
            .Bind(configuration.GetSection("AdminApi"))
            .ValidateOnStart();
        services.AddOptions<PublicMenuApiOptions>()
            .Bind(configuration.GetSection("PublicApi"))
            .ValidateOnStart();

        services.AddHttpClient(AccountSetupConstants.ApiClientName, (serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AdminApiOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        }).ConfigureOutboundHttpTimeout();

        services.AddHttpClient(AdminAuthenticationConstants.AdminApiClientName, (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AdminApiOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .ConfigureOutboundHttpTimeout()
            .AddHttpMessageHandler<AdminApiAuthenticationHandler>();

        services.AddHttpClient("ProbeClient", (serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PublicMenuApiOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        }).ConfigureOutboundHttpTimeout();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ValidateOptionsResult Validate(OutboundHttpClientOptions options)
    {
        return new OutboundHttpClientOptionsValidator().Validate(null, options);
    }

    private static void AssertValidationSucceeded(ValidateOptionsResult result)
    {
        Assert.False(result.Failed, result.FailureMessage);
    }

    private static void AssertValidationFailed(ValidateOptionsResult result, string expectedFailure)
    {
        Assert.True(result.Failed);
        Assert.Contains(expectedFailure, string.Join(" ", result.Failures), StringComparison.Ordinal);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ObservedCancellationToken = cancellationToken;
            Requests.Add(request);

            if (request.Content is not null)
            {
                _ = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return _response;
        }
    }

    private sealed class HangingHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class WaitingHttpMessageHandler : HttpMessageHandler
    {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ObservedCancellationToken = cancellationToken;
            RequestStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class StubAdminSessionTokenStore : IAdminSessionTokenStore
    {
        public Task StoreAsync(AdminSessionTokens tokens, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<AdminSessionTokens?> GetAsync(string sessionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminSessionTokens?>(null);
        }

        public Task RemoveAsync(string sessionId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<AdminSessionTokens?> RefreshAsync(
            string sessionId,
            DateTimeOffset refreshIfExpiresBefore,
            Func<AdminSessionTokens, CancellationToken, Task<AdminSessionTokens?>> refreshOperation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminSessionTokens?>(null);
        }
    }

    private sealed class OutboundHttpWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

        public OutboundHttpWebApplicationFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
        {
            _configurationOverrides = configurationOverrides;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(BuildConfiguration());
            });
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
        }

        private IReadOnlyDictionary<string, string?> BuildConfiguration()
        {
            var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AdminApi:BaseUrl"] = "http://localhost:5279",
                ["PublicApi:BaseUrl"] = "http://localhost:5279",
                ["PublicMenu:BaseUrl"] = "http://localhost:5161",
                ["AdminSession:Provider"] = AdminSessionProvider.Memory,
                ["AdminSession:MinimumCacheTtlSeconds"] = "1"
            };

            foreach (var item in _configurationOverrides)
            {
                configuration[item.Key] = item.Value;
            }

            return configuration;
        }
    }
}
