extern alias CafeMenuWeb;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AccountSetup;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AccountSetupBlazorTests
{
    private const string SetupToken = "setup-token-value-123456789012345";
    private const string ValidPassword = "SecurePassword123!";
    private const string InternalApiDetail = "database stack trace with token internals";

    [Fact]
    public async Task AccountSetup_ShouldBePublicNoStoreStaticSsrFormWithSingleAntiforgeryToken()
    {
        await using var factory = new AccountSetupWebApplicationFactory(new FakeAccountSetupApiClient());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/account/setup");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sifrenizi belirleyin", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("id=\"CompleteAccountSetupForm\"", html, StringComparison.Ordinal);
        Assert.Contains("method=\"post\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountAntiforgeryTokenInputs(ExtractFormHtml(html, "CompleteAccountSetupForm")));
        Assert.Contains("type=\"password\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("autocomplete=\"new-password\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("autocomplete=\"off\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/account/login?returnUrl=", response.RequestMessage?.RequestUri?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AccountSetup_Post_ShouldSendFormBodyToCompleteUserSetupAndRedirectToLoginSuccess()
    {
        var setupClient = new FakeAccountSetupApiClient(AccountSetupResult.Success());
        await using var factory = new AccountSetupWebApplicationFactory(setupClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var page = await client.GetStringAsync("/account/setup");
        var form = ExtractFormFields(page, "CompleteAccountSetupForm");
        form["_formModel.Token"] = SetupToken;
        form["_formModel.Password"] = ValidPassword;
        form["_formModel.ConfirmPassword"] = ValidPassword;

        using var response = await client.PostAsync("/account/setup", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/account/login?setup=success", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(1, setupClient.CompleteCallCount);
        Assert.Equal(new AccountSetupRequest(SetupToken, ValidPassword, ValidPassword), setupClient.LastRequest);
        Assert.DoesNotContain(SetupToken, response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidPassword, response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountSetup_PasswordMismatch_ShouldNotCallApiAndShouldNotEchoSensitiveValues()
    {
        var setupClient = new FakeAccountSetupApiClient(AccountSetupResult.Success());
        await using var factory = new AccountSetupWebApplicationFactory(setupClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var page = await client.GetStringAsync("/account/setup");
        var form = ExtractFormFields(page, "CompleteAccountSetupForm");
        form["_formModel.Token"] = SetupToken;
        form["_formModel.Password"] = ValidPassword;
        form["_formModel.ConfirmPassword"] = "DifferentPassword123!";

        using var response = await client.PostAsync("/account/setup", new FormUrlEncodedContent(form));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, setupClient.CompleteCallCount);
        Assert.Contains("Form alanlarini kontrol edin", html, StringComparison.Ordinal);
        Assert.DoesNotContain(SetupToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidPassword, html, StringComparison.Ordinal);
        Assert.DoesNotContain("DifferentPassword123!", html, StringComparison.Ordinal);
        Assert.Equal(1, CountAntiforgeryTokenInputs(ExtractFormHtml(html, "CompleteAccountSetupForm")));
    }

    [Theory]
    [InlineData(AccountSetupStatus.InvalidToken, "Setup kodu gecersiz")]
    [InlineData(AccountSetupStatus.ValidationError, "Sifre kurallarini kontrol edin")]
    [InlineData(AccountSetupStatus.Failure, "Sifre belirleme islemi su anda tamamlanamiyor")]
    public async Task AccountSetup_ApiFailures_ShouldRenderSafeMessagesWithoutSensitiveValues(
        AccountSetupStatus status,
        string expectedMessage)
    {
        var setupClient = new FakeAccountSetupApiClient(AccountSetupResult.Failure(status));
        await using var factory = new AccountSetupWebApplicationFactory(setupClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var page = await client.GetStringAsync("/account/setup");
        var form = ExtractFormFields(page, "CompleteAccountSetupForm");
        form["_formModel.Token"] = SetupToken;
        form["_formModel.Password"] = ValidPassword;
        form["_formModel.ConfirmPassword"] = ValidPassword;

        using var response = await client.PostAsync("/account/setup", new FormUrlEncodedContent(form));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedMessage, html, StringComparison.Ordinal);
        Assert.DoesNotContain(SetupToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidPassword, html, StringComparison.Ordinal);
        Assert.DoesNotContain(InternalApiDetail, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginPage_ShouldRenderSetupSuccessMessageWithoutChangingLoginForm()
    {
        await using var factory = new AccountSetupWebApplicationFactory(new FakeAccountSetupApiClient());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/account/login?setup=success");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hesabiniz hazir. Simdi giris yapabilirsiniz.", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/account/login\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"__RequestVerificationToken\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(SetupToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidPassword, html, StringComparison.Ordinal);
    }

    [Fact]
    public void AccountSetupSource_ShouldNotUseBrowserStorageRawMarkupOrTokenUrls()
    {
        var root = FindRepositoryRoot();
        var accountSetupSource = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(root, "src", "CafeMenu.Web"), "*AccountSetup*", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "src", "CafeMenu.Web", "AccountSetup"), "*.cs", SearchOption.AllDirectories))
                .Distinct()
                .Select(File.ReadAllText));

        Assert.DoesNotContain("localStorage", accountSetupSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", accountSetupSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", accountSetupSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("account/setup?token", accountSetupSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("account/setup/{token", accountSetupSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", accountSetupSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FormName=\"CompleteAccountSetupForm\"", accountSetupSource, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromForm(FormName = \"CompleteAccountSetupForm\", Name = \"_formModel\")]", accountSetupSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountSetupApiClient_ShouldCallExistingCompleteUserSetupEndpointWithoutBearerToken()
    {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "id": 10,
                    "email": "owner@example.local",
                    "fullName": "Owner User",
                    "isActive": true
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        using var httpClient = new HttpClient(handler);
        var client = new AccountSetupApiClient(
            new RecordingHttpClientFactory(httpClient),
            Options.Create(new AdminApiOptions { BaseUrl = "https://api.example.test/" }));

        var result = await client.CompleteUserSetupAsync(
            new AccountSetupRequest(SetupToken, ValidPassword, ValidPassword),
            CancellationToken.None);

        Assert.Equal(AccountSetupStatus.Success, result.Status);
        Assert.Equal("https://api.example.test/PlatformUser/CompleteUserSetup", handler.Requests[0].RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Null(handler.Requests[0].Headers.Authorization);
        Assert.Contains("\"token\":\"setup-token-value-123456789012345\"", handler.RequestBodies[0], StringComparison.Ordinal);
        Assert.Contains("\"password\":\"SecurePassword123!\"", handler.RequestBodies[0], StringComparison.Ordinal);
        Assert.Contains("\"confirmPassword\":\"SecurePassword123!\"", handler.RequestBodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("Authentication/Login", handler.Requests[0].RequestUri?.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AccountSetupStatus.InvalidToken)]
    [InlineData(HttpStatusCode.BadRequest, AccountSetupStatus.ValidationError)]
    [InlineData(HttpStatusCode.InternalServerError, AccountSetupStatus.Failure)]
    public async Task AccountSetupApiClient_ShouldMapFailureStatusCodes(HttpStatusCode statusCode, AccountSetupStatus expectedStatus)
    {
        using var httpClient = new HttpClient(new RecordingHttpMessageHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(InternalApiDetail, Encoding.UTF8, "text/plain")
        }));
        var client = new AccountSetupApiClient(
            new RecordingHttpClientFactory(httpClient),
            Options.Create(new AdminApiOptions { BaseUrl = "https://api.example.test/" }));

        var result = await client.CompleteUserSetupAsync(
            new AccountSetupRequest(SetupToken, ValidPassword, ValidPassword),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
    }

    private static Dictionary<string, string> ExtractFormFields(string html, string formId)
    {
        var formHtml = ExtractFormHtml(html, formId);
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match inputMatch in Regex.Matches(formHtml, "<input[^>]*>", RegexOptions.CultureInvariant))
        {
            var input = inputMatch.Value;
            var name = GetAttributeValue(input, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            fields[name] = WebUtility.HtmlDecode(GetAttributeValue(input, "value") ?? string.Empty);
        }

        return fields;
    }

    private static string ExtractFormHtml(string html, string formId)
    {
        var formMatch = Regex.Match(
            html,
            $@"<form(?=[^>]*id=""{Regex.Escape(formId)}"")[\s\S]*?</form>",
            RegexOptions.CultureInvariant);

        Assert.True(formMatch.Success, html);
        return formMatch.Value;
    }

    private static string? GetAttributeValue(string tag, string attributeName)
    {
        var match = Regex.Match(
            tag,
            $@"\s{Regex.Escape(attributeName)}=""(?<value>[^""]*)""",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["value"].Value : null;
    }

    private static int CountAntiforgeryTokenInputs(string formHtml)
    {
        return Regex.Matches(
            formHtml,
            @"<input(?=[^>]*name=""__RequestVerificationToken"")[^>]*>",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Count;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "CafeMenu.Web",
                "Components",
                "Pages",
                "AccountSetupPage.razor");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located.");
    }

    private sealed class FakeAccountSetupApiClient : IAccountSetupApiClient
    {
        private readonly AccountSetupResult _result;

        public FakeAccountSetupApiClient(AccountSetupResult? result = null)
        {
            _result = result ?? AccountSetupResult.Failure(AccountSetupStatus.Failure);
        }

        public int CompleteCallCount { get; private set; }

        public AccountSetupRequest? LastRequest { get; private set; }

        public Task<AccountSetupResult> CompleteUserSetupAsync(
            AccountSetupRequest request,
            CancellationToken cancellationToken)
        {
            CompleteCallCount++;
            LastRequest = request;

            return Task.FromResult(_result);
        }
    }

    private sealed class AccountSetupWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAccountSetupApiClient _accountSetupApiClient;

        public AccountSetupWebApplicationFactory(IAccountSetupApiClient accountSetupApiClient)
        {
            _accountSetupApiClient = accountSetupApiClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountSetupApiClient>();
                services.AddSingleton(_accountSetupApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-account-setup-test-data-protection"));
                services.AddDataProtection()
                    .PersistKeysToFileSystem(keyDirectory);
            });
        }
    }

    private sealed class StubPublicMenuApiClient : IPublicMenuApiClient
    {
        public Task<PublicMenuRequestResult> GetMenuAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(PublicMenuRequestResult.NotFound());
        }
    }

    private sealed class RecordingHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public RecordingHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            Assert.Equal(AccountSetupConstants.ApiClientName, name);
            return _httpClient;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return _response;
        }
    }
}
