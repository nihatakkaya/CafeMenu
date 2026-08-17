extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CafeMenu.Api.Data;
using CafeMenu.Shared.RateLimiting;
using CafeMenu.Shared.SecurityHeaders;
using CafeMenuWeb::CafeMenu.Web.AccountSetup;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class RateLimitingTests
{
    private const string LoginEmail = "limited@example.local";
    private const string ValidSetupToken = "setup-token-value-123456789012345";
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public void Validator_ShouldAllowDefaultPolicyConfiguration()
    {
        var result = new ApplicationRateLimitingOptionsValidator()
            .Validate(null, new ApplicationRateLimitingOptions());

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldRejectNonPositivePermitLimit(int permitLimit)
    {
        var result = new ApplicationRateLimitingOptionsValidator()
            .Validate(null, new ApplicationRateLimitingOptions
            {
                Login = new RateLimitPolicyOptions
                {
                    PermitLimit = permitLimit,
                    WindowSeconds = 60
                }
            });

        AssertValidationFailed(result, "RateLimiting:Login:PermitLimit");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldRejectNonPositiveWindowSeconds(int windowSeconds)
    {
        var result = new ApplicationRateLimitingOptionsValidator()
            .Validate(null, new ApplicationRateLimitingOptions
            {
                Login = new RateLimitPolicyOptions
                {
                    PermitLimit = 10,
                    WindowSeconds = windowSeconds
                }
            });

        AssertValidationFailed(result, "RateLimiting:Login:WindowSeconds");
    }

    [Fact]
    public async Task ApiLogin_ShouldContinueNormalBehaviorBelowLimitAndReturn429AfterLimit()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        });

        var first = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"limited@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.10"));
        var second = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"limited@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.10"));

        Assert.Equal(StatusCodes.Status401Unauthorized, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
        Assert.Contains("Too many requests", second.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(LoginEmail, second.Body, StringComparison.OrdinalIgnoreCase);
        Assert.True(second.Response.Headers.ContainsKey("Retry-After"));
        AssertSecurityHeaders(second.Response);
    }

    [Fact]
    public async Task ApiLogin_ShouldPartitionByRemoteIpAddress()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        });

        var firstIpFirst = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"one@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.20"));
        var firstIpSecond = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"one@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.20"));
        var secondIpFirst = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"two@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.21"));

        Assert.Equal(StatusCodes.Status401Unauthorized, firstIpFirst.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, firstIpSecond.Response.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, secondIpFirst.Response.StatusCode);
    }

    [Fact]
    public async Task ApiLogin_ShouldUseTrustedForwardedClientIpForPartitioning()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["ReverseProxy:Enabled"] = "true",
            ["ReverseProxy:KnownIPNetworks:0"] = "::1/128",
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        });

        var firstForwardedIpFirst = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"one@example.local","password":"wrong-password"}""",
            IPAddress.IPv6Loopback,
            "203.0.113.30");
        var firstForwardedIpSecond = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"one@example.local","password":"wrong-password"}""",
            IPAddress.IPv6Loopback,
            "203.0.113.30");
        var secondForwardedIpFirst = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"two@example.local","password":"wrong-password"}""",
            IPAddress.IPv6Loopback,
            "203.0.113.31");

        Assert.Equal(StatusCodes.Status401Unauthorized, firstForwardedIpFirst.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, firstForwardedIpSecond.Response.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, secondForwardedIpFirst.Response.StatusCode);
    }

    [Fact]
    public async Task ApiRefreshToken_ShouldReturn429AfterLimit()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Refresh:PermitLimit"] = "1",
            ["RateLimiting:Refresh:WindowSeconds"] = "60"
        });

        var first = await SendApiJsonAsync(
            factory,
            "/Authentication/RefreshToken",
            """{"refreshToken":"invalid-refresh-token"}""",
            IPAddress.Parse("203.0.113.40"));
        var second = await SendApiJsonAsync(
            factory,
            "/Authentication/RefreshToken",
            """{"refreshToken":"invalid-refresh-token"}""",
            IPAddress.Parse("203.0.113.40"));

        Assert.Equal(StatusCodes.Status401Unauthorized, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
    }

    [Fact]
    public async Task ApiCompleteUserSetup_ShouldReturn429AfterLimit()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:AccountSetup:PermitLimit"] = "1",
            ["RateLimiting:AccountSetup:WindowSeconds"] = "60"
        });

        var body = $$"""{"token":"{{ValidSetupToken}}","password":"{{ValidPassword}}","confirmPassword":"{{ValidPassword}}"}""";
        var first = await SendApiJsonAsync(
            factory,
            "/PlatformUser/CompleteUserSetup",
            body,
            IPAddress.Parse("203.0.113.50"));
        var second = await SendApiJsonAsync(
            factory,
            "/PlatformUser/CompleteUserSetup",
            body,
            IPAddress.Parse("203.0.113.50"));

        Assert.NotEqual(StatusCodes.Status429TooManyRequests, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
    }

    [Fact]
    public async Task ApiPlatformUserSetup_ShouldReturn429AfterLimit()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:PlatformUserSetup:PermitLimit"] = "1",
            ["RateLimiting:PlatformUserSetup:WindowSeconds"] = "60"
        });

        var first = await SendApiJsonAsync(
            factory,
            "/PlatformUser/CreateUserSetup",
            """{"email":"owner@example.local","fullName":"Owner User"}""",
            IPAddress.Parse("203.0.113.60"));
        var second = await SendApiJsonAsync(
            factory,
            "/PlatformUser/CreateUserSetup",
            """{"email":"owner@example.local","fullName":"Owner User"}""",
            IPAddress.Parse("203.0.113.60"));

        Assert.Equal(StatusCodes.Status401Unauthorized, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
    }

    [Fact]
    public async Task PublicApiEndpoint_ShouldNotBeRateLimitedByAuthPolicy()
    {
        await using var factory = new RateLimitedApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        });

        _ = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"limited@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.70"));
        _ = await SendApiJsonAsync(
            factory,
            "/Authentication/Login",
            """{"email":"limited@example.local","password":"wrong-password"}""",
            IPAddress.Parse("203.0.113.70"));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/System/Health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WebLoginPost_ShouldReturn429AfterLimit()
    {
        await using var factory = new RateLimitedWebFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "60"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await GetLoginAntiforgeryTokenAsync(client);
        var first = await PostLoginAsync(client, token);
        var second = await PostLoginAsync(client, token);

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task WebAccountSetupPost_ShouldReturn429AfterLimitWithoutGetConsumingPermit()
    {
        await using var factory = new RateLimitedWebFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:AccountSetup:PermitLimit"] = "1",
            ["RateLimiting:AccountSetup:WindowSeconds"] = "60"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var html = await client.GetStringAsync("/account/setup");
        var form = ExtractAccountSetupFormFields(html);
        form["_formModel.Token"] = ValidSetupToken;
        form["_formModel.Password"] = ValidPassword;
        form["_formModel.ConfirmPassword"] = ValidPassword;

        var first = await client.PostAsync("/account/setup", new FormUrlEncodedContent(form));
        var second = await client.PostAsync("/account/setup", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    private static async Task<ApiSendResult> SendApiJsonAsync(
        WebApplicationFactory<Program> factory,
        string path,
        string body,
        IPAddress remoteIpAddress,
        string? forwardedFor = null)
    {
        var context = await factory.Server.SendAsync(httpContext =>
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            httpContext.Request.Method = HttpMethods.Post;
            httpContext.Request.Path = path;
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("api.example.test");
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.ContentLength = bodyBytes.Length;
            httpContext.Request.Body = new MemoryStream(bodyBytes);
            httpContext.Connection.RemoteIpAddress = remoteIpAddress;

            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                httpContext.Request.Headers["X-Forwarded-For"] = forwardedFor;
                httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
            }
        });

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        var responseBody = await reader.ReadToEndAsync();

        return new ApiSendResult(context.Response, responseBody);
    }

    private static async Task<string> GetLoginAntiforgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/account/login");
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, html);
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string antiforgeryToken)
    {
        return client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner@example.local",
                ["Password"] = "wrong-password",
                ["__RequestVerificationToken"] = antiforgeryToken
            }));
    }

    private static Dictionary<string, string> ExtractAccountSetupFormFields(string html)
    {
        var formMatch = Regex.Match(
            html,
            "<form[^>]*id=\"CompleteAccountSetupForm\"[^>]*>(?<form>.*?)</form>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(formMatch.Success, html);

        return Regex.Matches(
                formMatch.Groups["form"].Value,
                "<input[^>]*name=\"(?<name>[^\"]+)\"[^>]*>",
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .GroupBy(match => WebUtility.HtmlDecode(match.Groups["name"].Value), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => WebUtility.HtmlDecode(GetInputValue(group.First().Value)),
                StringComparer.Ordinal);
    }

    private static string GetInputValue(string inputHtml)
    {
        var valueMatch = Regex.Match(
            inputHtml,
            "value=\"(?<value>[^\"]*)\"",
            RegexOptions.CultureInvariant);

        return valueMatch.Success ? valueMatch.Groups["value"].Value : string.Empty;
    }

    private static void AssertSecurityHeaders(HttpResponse response)
    {
        Assert.Equal(
            ApplicationSecurityHeaders.XContentTypeOptionsValue,
            response.Headers[ApplicationSecurityHeaders.XContentTypeOptionsHeaderName]);
        Assert.Equal(
            ApplicationSecurityHeaders.ReferrerPolicyValue,
            response.Headers[ApplicationSecurityHeaders.ReferrerPolicyHeaderName]);
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

    private sealed record ApiSendResult(HttpResponse Response, string Body);

    private sealed class RateLimitedApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"cafemenu_rate_limit_tests_{Guid.NewGuid():N}";
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

        public RateLimitedApiFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
        {
            _configurationOverrides = configurationOverrides;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["AllowedHosts"] = "api.example.test;localhost",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=cafemenu_test;Username=test;Password=test",
                    ["ImageStorage:Provider"] = "Local",
                    ["ImageStorage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "cafemenu-tests-media", _databaseName),
                    ["ImageStorage:PublicBaseUrl"] = "http://localhost/media",
                    ["ImageStorage:MaxFileSizeBytes"] = "5242880"
                };

                foreach (var item in _configurationOverrides)
                {
                    configuration[item.Key] = item.Value;
                }

                configurationBuilder.AddInMemoryCollection(configuration);
            });
            builder.ConfigureTestServices(services =>
            {
                var dbContextDescriptors = services
                    .Where(descriptor =>
                        descriptor.ServiceType == typeof(CafeMenuDbContext) ||
                        descriptor.ServiceType == typeof(DbContextOptions) ||
                        descriptor.ServiceType == typeof(DbContextOptions<CafeMenuDbContext>) ||
                        descriptor.ServiceType.Name.Contains("DbContextOptionsConfiguration", StringComparison.Ordinal))
                    .ToArray();

                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<CafeMenuDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        }
    }

    private sealed class RateLimitedWebFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

        public RateLimitedWebFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
        {
            _configurationOverrides = configurationOverrides;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(_configurationOverrides);
            });
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountSetupApiClient>();
                services.AddSingleton<IAccountSetupApiClient>(new StubAccountSetupApiClient());
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(
                    Path.GetTempPath(),
                    "cafemenu-rate-limiting-web-data-protection",
                    Guid.NewGuid().ToString("N")));
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }
    }

    private sealed class StubAccountSetupApiClient : IAccountSetupApiClient
    {
        public Task<AccountSetupResult> CompleteUserSetupAsync(
            AccountSetupRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AccountSetupResult.Failure(AccountSetupStatus.InvalidToken));
        }
    }

    private sealed class StubPublicMenuApiClient : IPublicMenuApiClient
    {
        public Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(PublicMenuRequestResult.NotFound());
        }

        public Task<PublicProductDetailRequestResult> GetProductDetailAsync(
            string slug,
            long productId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PublicProductDetailRequestResult.NotFound());
        }
    }
}
