extern alias CafeMenuWeb;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AdminAuthenticationInfrastructureTests
{
    private const string SessionId = "test-session";
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";
    private const string RotatedAccessToken = "rotated-access-token-value";
    private const string RotatedRefreshToken = "rotated-refresh-token-value";

    [Fact]
    public async Task LoginAsync_ShouldStoreApiTokensServerSide()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        var authClient = new FakeAdminAuthApiClient(CreateAuthResponse());
        var service = new AdminAuthService(authClient, tokenStore);

        var result = await service.LoginAsync(
            new AdminLoginCommand("owner@example.local", "SecurePassword123!"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var sessionId = result.Principal?.FindFirst(AdminAuthenticationConstants.SessionIdClaim)?.Value;
        Assert.False(string.IsNullOrWhiteSpace(sessionId));

        var storedTokens = await tokenStore.GetAsync(sessionId!, CancellationToken.None);
        Assert.NotNull(storedTokens);
        Assert.Equal(AccessToken, storedTokens.AccessToken);
        Assert.Equal(RefreshToken, storedTokens.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_ShouldCreateNewSessionIdentifierForEachLogin()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        var authClient = new FakeAdminAuthApiClient(CreateAuthResponse());
        var service = new AdminAuthService(authClient, tokenStore);

        var first = await service.LoginAsync(
            new AdminLoginCommand("owner@example.local", "SecurePassword123!"),
            CancellationToken.None);
        var second = await service.LoginAsync(
            new AdminLoginCommand("owner@example.local", "SecurePassword123!"),
            CancellationToken.None);

        var firstSessionId = first.Principal?.FindFirst(AdminAuthenticationConstants.SessionIdClaim)?.Value;
        var secondSessionId = second.Principal?.FindFirst(AdminAuthenticationConstants.SessionIdClaim)?.Value;

        Assert.False(string.IsNullOrWhiteSpace(firstSessionId));
        Assert.False(string.IsNullOrWhiteSpace(secondSessionId));
        Assert.NotEqual(firstSessionId, secondSessionId);
    }

    [Fact]
    public async Task LoginAsync_ShouldNotCreateSessionWhenApiLoginFails()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        var authClient = new FakeAdminAuthApiClient(loginResponse: null);
        var service = new AdminAuthService(authClient, tokenStore);

        var result = await service.LoginAsync(
            new AdminLoginCommand("owner@example.local", "wrong-password"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task Handler_ShouldAddBearerAccessTokenHeader()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        await tokenStore.StoreAsync(CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10)), CancellationToken.None);
        var innerHandler = new RecordingHttpMessageHandler();
        using var httpClient = CreateAdminHttpClient(tokenStore, new FakeAdminAuthApiClient(), innerHandler);

        using var response = await httpClient.GetAsync("Cafe/GetCafeById/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", innerHandler.Authorization?.Scheme);
        Assert.Equal(AccessToken, innerHandler.Authorization?.Parameter);
    }

    [Fact]
    public async Task Handler_ShouldRefreshNearExpiryAccessTokenAndStoreRotatedTokens()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        await tokenStore.StoreAsync(CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddSeconds(10)), CancellationToken.None);
        var authClient = new FakeAdminAuthApiClient(refreshResponse: CreateAuthResponse(RotatedAccessToken, RotatedRefreshToken));
        var innerHandler = new RecordingHttpMessageHandler();
        using var httpClient = CreateAdminHttpClient(tokenStore, authClient, innerHandler);

        using var response = await httpClient.GetAsync("Cafe/GetCafeById/1");

        var storedTokens = await tokenStore.GetAsync(SessionId, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, authClient.RefreshCallCount);
        Assert.Equal(RotatedAccessToken, innerHandler.Authorization?.Parameter);
        Assert.Equal(RotatedAccessToken, storedTokens?.AccessToken);
        Assert.Equal(RotatedRefreshToken, storedTokens?.RefreshToken);
    }

    [Fact]
    public async Task Handler_ShouldCoalesceParallelRefreshesForSameSession()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        await tokenStore.StoreAsync(CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddSeconds(10)), CancellationToken.None);
        var authClient = new FakeAdminAuthApiClient(
            refreshResponse: CreateAuthResponse(RotatedAccessToken, RotatedRefreshToken),
            refreshDelay: TimeSpan.FromMilliseconds(50));
        using var httpClient = CreateAdminHttpClient(tokenStore, authClient, new RecordingHttpMessageHandler());

        var requests = Enumerable.Range(0, 5)
            .Select(_ => httpClient.GetAsync("Cafe/GetCafeById/1"))
            .ToArray();
        using var response1 = await requests[0];
        using var response2 = await requests[1];
        using var response3 = await requests[2];
        using var response4 = await requests[3];
        using var response5 = await requests[4];

        Assert.All([response1, response2, response3, response4, response5], response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(1, authClient.RefreshCallCount);
    }

    [Fact]
    public async Task Handler_ShouldInvalidateSessionWhenRefreshFails()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        await tokenStore.StoreAsync(CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddSeconds(10)), CancellationToken.None);
        var authClient = new FakeAdminAuthApiClient(refreshResponse: null);
        var innerHandler = new RecordingHttpMessageHandler();
        using var httpClient = CreateAdminHttpClient(tokenStore, authClient, innerHandler);

        using var response = await httpClient.GetAsync("Cafe/GetCafeById/1");

        var storedTokens = await tokenStore.GetAsync(SessionId, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(storedTokens);
        Assert.Null(innerHandler.Authorization);
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallBackendRevokeAndClearStore()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        await tokenStore.StoreAsync(CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10)), CancellationToken.None);
        var authClient = new FakeAdminAuthApiClient();
        var service = new AdminAuthService(authClient, tokenStore);

        await service.LogoutAsync(CreatePrincipal(), CancellationToken.None);

        var storedTokens = await tokenStore.GetAsync(SessionId, CancellationToken.None);
        Assert.Equal(1, authClient.LogoutCallCount);
        Assert.Equal(RefreshToken, authClient.LastLogoutRefreshToken);
        Assert.Null(storedTokens);
    }

    [Fact]
    public async Task MemoryStore_ShouldCreateGetUpdateDeleteAndIsolateSessions()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        var firstSession = CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10));
        var secondSession = firstSession with
        {
            SessionId = "second-session",
            AccessToken = "second-access-token",
            RefreshToken = "second-refresh-token"
        };

        await tokenStore.StoreAsync(firstSession, CancellationToken.None);
        await tokenStore.StoreAsync(secondSession, CancellationToken.None);
        await tokenStore.StoreAsync(firstSession with { AccessToken = RotatedAccessToken }, CancellationToken.None);
        await tokenStore.RemoveAsync(secondSession.SessionId, CancellationToken.None);

        var firstStored = await tokenStore.GetAsync(firstSession.SessionId, CancellationToken.None);
        var secondStored = await tokenStore.GetAsync(secondSession.SessionId, CancellationToken.None);

        Assert.Equal(RotatedAccessToken, firstStored?.AccessToken);
        Assert.Null(secondStored);
    }

    [Fact]
    public async Task MemoryStore_ShouldNotReturnExpiredSession()
    {
        var tokenStore = new MemoryAdminSessionTokenStore();
        var expiredTokens = CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-10)) with
        {
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1)
        };

        await tokenStore.StoreAsync(expiredTokens, CancellationToken.None);

        var storedTokens = await tokenStore.GetAsync(SessionId, CancellationToken.None);

        Assert.Null(storedTokens);
    }

    [Fact]
    public async Task RedisStore_ShouldShareSessionsAcrossStoreInstances()
    {
        var cache = CreateSharedDistributedCache();
        var firstStore = CreateRedisStore(cache);
        var secondStore = CreateRedisStore(cache);
        var tokens = CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10));

        await firstStore.StoreAsync(tokens, CancellationToken.None);

        var storedTokens = await secondStore.GetAsync(SessionId, CancellationToken.None);

        Assert.NotNull(storedTokens);
        Assert.Equal(AccessToken, storedTokens.AccessToken);
        Assert.Equal(RefreshToken, storedTokens.RefreshToken);
    }

    [Fact]
    public async Task RedisStore_ShouldUpdateAndRemoveSession()
    {
        var cache = CreateSharedDistributedCache();
        var store = CreateRedisStore(cache);
        await store.StoreAsync(CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddSeconds(1)), CancellationToken.None);

        var refreshed = await store.RefreshAsync(
            SessionId,
            DateTimeOffset.UtcNow.AddMinutes(1),
            (_, _) => Task.FromResult<AdminSessionTokens?>(
                CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(30)) with
                {
                    AccessToken = RotatedAccessToken,
                    RefreshToken = RotatedRefreshToken
                }),
            CancellationToken.None);

        await store.RemoveAsync(SessionId, CancellationToken.None);
        var removed = await store.GetAsync(SessionId, CancellationToken.None);

        Assert.Equal(RotatedAccessToken, refreshed?.AccessToken);
        Assert.Null(removed);
    }

    [Fact]
    public async Task RedisStore_ShouldRejectMissingExpiredAndMalformedSessions()
    {
        var cache = CreateSharedDistributedCache();
        var store = CreateRedisStore(cache);
        var expiredTokens = CreateTokens(accessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-10)) with
        {
            SessionId = "expired-session",
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1)
        };
        await store.StoreAsync(expiredTokens, CancellationToken.None);
        await cache.SetStringAsync(
            BuildRedisKey("malformed-session"),
            "{not-json",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        var missing = await store.GetAsync("missing-session", CancellationToken.None);
        var expired = await store.GetAsync("expired-session", CancellationToken.None);
        var malformed = await store.GetAsync("malformed-session", CancellationToken.None);

        Assert.Null(missing);
        Assert.Null(expired);
        Assert.Null(malformed);
    }

    [Fact]
    public async Task LoginEndpoint_ShouldIssueCookieWithoutJwtTokenValues()
    {
        var authClient = new FakeAdminAuthApiClient(CreateAuthResponse());
        await using var factory = new AdminAuthWebApplicationFactory(authClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var loginResponse = await client.GetAsync("/account/login");
        var loginPage = await loginResponse.Content.ReadAsStringAsync();

        Assert.True(loginResponse.IsSuccessStatusCode, loginPage);

        var antiforgeryToken = ExtractAntiforgeryToken(loginPage);
        using var response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner@example.local",
                ["Password"] = "SecurePassword123!",
                ["__RequestVerificationToken"] = antiforgeryToken
            }));

        var setCookieValues = response.Headers.GetValues("Set-Cookie").ToArray();
        var combinedCookies = string.Join("\n", setCookieValues);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(setCookieValues, value => value.StartsWith("CafeMenu.Admin=", StringComparison.Ordinal));
        Assert.DoesNotContain(AccessToken, combinedCookies, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, combinedCookies, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogoutForm_ShouldRenderForAuthenticatedUserWithPostAndAntiforgery()
    {
        var authClient = new FakeAdminAuthApiClient(CreateAuthResponse());
        await using var factory = new AdminAuthWebApplicationFactory(authClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client);

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("method=\"post\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action=\"/account/logout\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"__RequestVerificationToken\"", html, StringComparison.Ordinal);
        Assert.Contains("Çıkış", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain(">Giriş<", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("Yönetim panelinden cafelerinizi yönetin", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain("Cafe yönetimi için giriş yapın", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogoutForm_ShouldNotRenderForAnonymousUser()
    {
        await using var factory = new AdminAuthWebApplicationFactory(new FakeAdminAuthApiClient());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("action=\"/account/logout\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Logout<", html, StringComparison.Ordinal);
        Assert.Contains(">Giriş<", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("Cafe yönetimi için giriş yapın", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DevelopmentEnvironment_ShouldAllowMemoryAdminSessionTokenStore()
    {
        await using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ProductionEnvironment_ShouldFailFastWithMemoryAdminSessionTokenStore()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production");

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            AdminAuthServiceCollectionExtensions.MemoryStoreProductionGuardMessage);
    }

    [Fact]
    public void ProductionEnvironment_ShouldFailFastWithMissingRedisConfiguration()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production",
            new Dictionary<string, string?>
            {
                ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                ["AdminSession:RedisConnectionString"] = ""
            });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "AdminSession:RedisConnectionString is required");
    }

    [Fact]
    public void ProductionEnvironment_ShouldFailFastWithUnsupportedSessionProvider()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production",
            new Dictionary<string, string?>
            {
                ["AdminSession:Provider"] = "File"
            });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "AdminSession:Provider must be Memory or Redis");
    }

    [Fact]
    public void ProductionEnvironment_ShouldFailFastWithInvalidSessionTtl()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production",
            new Dictionary<string, string?>
            {
                ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                ["AdminSession:RedisConnectionString"] = "localhost:6379",
                ["AdminSession:MinimumCacheTtlSeconds"] = "0"
            });

        AssertStartupValidationContains(
            () => factory.CreateClient(),
            "MinimumCacheTtlSeconds");
    }

    [Fact]
    public void DevelopmentEnvironment_ShouldRegisterMemoryAdminSessionTokenStore()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Development");
        using var _ = factory.CreateClient();

        var store = factory.Services.GetRequiredService<IAdminSessionTokenStore>();

        Assert.IsType<MemoryAdminSessionTokenStore>(store);
    }

    [Fact]
    public void ProductionEnvironment_ShouldRegisterRedisAdminSessionTokenStore()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production",
            new Dictionary<string, string?>
            {
                ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                ["AdminSession:RedisConnectionString"] = "localhost:6379"
            });
        using var _ = factory.CreateClient();

        var store = factory.Services.GetRequiredService<IAdminSessionTokenStore>();

        Assert.IsType<RedisAdminSessionTokenStore>(store);
    }

    [Fact]
    public void CookieOptions_ShouldUseSecureHttpOnlySettings()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production",
            new Dictionary<string, string?>
            {
                ["AdminSession:Provider"] = AdminSessionProvider.Redis,
                ["AdminSession:RedisConnectionString"] = "localhost:6379"
            });
        using var _ = factory.CreateClient();

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(AdminAuthenticationConstants.CookieScheme);

        Assert.Equal("CafeMenu.Admin", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.False(options.SlidingExpiration);
    }

    [Fact]
    public void ProductionGuard_ShouldNotExposeTokenValues()
    {
        using var factory = new AdminAuthWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            "Production");

        var exception = AssertStartupValidationContains(
            () => factory.CreateClient(),
            AdminAuthServiceCollectionExtensions.MemoryStoreProductionGuardMessage);

        Assert.DoesNotContain(AccessToken, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(RotatedAccessToken, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(RotatedRefreshToken, exception.ToString(), StringComparison.Ordinal);
    }

    private static HttpClient CreateAdminHttpClient(
        IAdminSessionTokenStore tokenStore,
        IAdminAuthApiClient authClient,
        RecordingHttpMessageHandler innerHandler)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal()
            }
        };
        var handler = new AdminApiAuthenticationHandler(
            httpContextAccessor,
            tokenStore,
            authClient,
            TimeProvider.System)
        {
            InnerHandler = innerHandler
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
    }

    private static ClaimsPrincipal CreatePrincipal()
    {
        var identity = new ClaimsIdentity(
            [new Claim(AdminAuthenticationConstants.SessionIdClaim, SessionId)],
            AdminAuthenticationConstants.CookieScheme);

        return new ClaimsPrincipal(identity);
    }

    private static AdminSessionTokens CreateTokens(DateTimeOffset accessTokenExpiresAt)
    {
        return new AdminSessionTokens(
            SessionId,
            AccessToken,
            accessTokenExpiresAt,
            RefreshToken,
            DateTimeOffset.UtcNow.AddDays(7));
    }

    private static AdminAuthResponse CreateAuthResponse(
        string accessToken = AccessToken,
        string refreshToken = RefreshToken)
    {
        return new AdminAuthResponse(
            accessToken,
            refreshToken,
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow.AddDays(7),
            new AdminUserResponse(
                10,
                "owner@example.local",
                "Cafe Owner",
                ["CAFE_OWNER"]));
    }

    private static MemoryDistributedCache CreateSharedDistributedCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    private static RedisAdminSessionTokenStore CreateRedisStore(IDistributedCache cache)
    {
        return new RedisAdminSessionTokenStore(
            cache,
            Options.Create(new AdminSessionOptions
            {
                Provider = AdminSessionProvider.Redis,
                KeyPrefix = "test:admin-session:",
                RedisConnectionString = "localhost:6379",
                MinimumCacheTtlSeconds = 1
            }),
            TimeProvider.System);
    }

    private static string BuildRedisKey(string sessionId)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));
        return string.Concat("test:admin-session:", WebEncoders.Base64UrlEncode(digest));
    }

    private static Exception AssertStartupValidationContains(Action action, string expectedMessage)
    {
        var exception = Record.Exception(action);

        Assert.NotNull(exception);
        Assert.Contains(expectedMessage, exception.ToString(), StringComparison.Ordinal);
        return exception;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, html);
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static async Task LoginThroughEndpointAsync(HttpClient client)
    {
        using var loginResponse = await client.GetAsync("/account/login");
        var loginPage = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginPage);

        var antiforgeryToken = ExtractAntiforgeryToken(loginPage);
        using var response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner@example.local",
                ["Password"] = "SecurePassword123!",
                ["__RequestVerificationToken"] = antiforgeryToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class FakeAdminAuthApiClient : IAdminAuthApiClient
    {
        private readonly AdminAuthResponse? _loginResponse;
        private readonly AdminAuthResponse? _refreshResponse;
        private readonly TimeSpan _refreshDelay;

        public FakeAdminAuthApiClient(
            AdminAuthResponse? loginResponse = null,
            AdminAuthResponse? refreshResponse = null,
            TimeSpan refreshDelay = default)
        {
            _loginResponse = loginResponse;
            _refreshResponse = refreshResponse;
            _refreshDelay = refreshDelay;
        }

        public int RefreshCallCount { get; private set; }

        public int LogoutCallCount { get; private set; }

        public string? LastLogoutRefreshToken { get; private set; }

        public Task<AdminAuthResponse?> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResponse);
        }

        public async Task<AdminAuthResponse?> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            RefreshCallCount++;

            if (_refreshDelay > TimeSpan.Zero)
            {
                await Task.Delay(_refreshDelay, cancellationToken);
            }

            return _refreshResponse;
        }

        public Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LogoutCallCount++;
            LastLogoutRefreshToken = refreshToken;
            return Task.FromResult(true);
        }
    }

    private sealed class AdminAuthWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly string? _environmentName;
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly string _dataProtectionKeyPath = Path.Combine(
            Path.GetTempPath(),
            "cafemenu-web-auth-test-data-protection",
            Guid.NewGuid().ToString("N"));

        public AdminAuthWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            string? environmentName = null,
            IReadOnlyDictionary<string, string?>? configurationOverrides = null)
        {
            _authApiClient = authApiClient;
            _environmentName = environmentName;
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(_environmentName))
            {
                builder.UseEnvironment(_environmentName);
            }

            if (_configurationOverrides.Count > 0 ||
                string.Equals(_environmentName, "Production", StringComparison.Ordinal))
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(BuildConfigurationOverrides());
                });
            }

            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthApiClient>();
                services.AddSingleton(_authApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-web-auth-test-data-protection"));
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }

        private IReadOnlyDictionary<string, string?> BuildConfigurationOverrides()
        {
            var overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AdminApi:BaseUrl"] = "https://api.example.test",
                ["PublicApi:BaseUrl"] = "https://api.example.test",
                ["PublicMenu:BaseUrl"] = "https://menu.example.test",
                ["AllowedHosts"] = "localhost",
                ["DataProtection:ApplicationName"] = "CafeMenu.Web.Tests",
                ["DataProtection:KeyRingPath"] = _dataProtectionKeyPath
            };

            foreach (var item in _configurationOverrides)
            {
                overrides[item.Key] = item.Value;
            }

            return overrides;
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
