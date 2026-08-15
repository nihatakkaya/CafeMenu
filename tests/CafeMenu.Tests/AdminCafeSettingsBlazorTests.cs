extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminCafeSettings;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AdminCafeSettingsBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public async Task SettingsRoute_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin/cafes/10/settings");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/account/login?returnUrl=%2Fadmin%2Fcafes%2F10%2Fsettings",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AccessibleCafe_ShouldOpenSettingsPageAndLoadBackendCafe()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");

        using var response = await client.GetAsync("/admin/cafes/10/settings");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, settingsClient.GetSettingsCallCount);
        Assert.Equal(10, settingsClient.LastGetCafeId);
        Assert.Contains("Cafe Ayarları", html, StringComparison.Ordinal);
        Assert.Contains("Settings Cafe", html, StringComparison.Ordinal);
        Assert.Contains("/c/settings-cafe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InaccessibleCafe_ShouldRejectBeforeSettingsApiCall()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/settings");

        using var response = await client.GetAsync("/admin/cafes/99/settings");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.Equal(0, settingsClient.GetSettingsCallCount);
        Assert.Equal(0, settingsClient.UpdateSettingsCallCount);
    }

    [Fact]
    public async Task ExistingValues_ShouldBindToSettingsForm()
    {
        var settings = CreateSettings(name: "Loaded Cafe", slug: "loaded-cafe");
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(settings)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");

        using var response = await client.GetAsync("/admin/cafes/10/settings");
        var fields = ExtractFormFields(await response.Content.ReadAsStringAsync(), "UpdateCafeSettingsForm");

        AssertFormField(fields, "Name", "Loaded Cafe");
        AssertFormField(fields, "Slug", "loaded-cafe");
    }

    [Fact]
    public async Task SavePost_ShouldBindSubmittedValuesAndUseRouteCafeId()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");
        using var getResponse = await client.GetAsync("/admin/cafes/10/settings");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "UpdateCafeSettingsForm");
        SetFormField(formFields, "Name", "Yeni Cafe");
        SetFormField(formFields, "Slug", "yeni-cafe");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/settings",
            new FormUrlEncodedContent(formFields));
        var postHtml = WebUtility.HtmlDecode(await postResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(1, settingsClient.UpdateSettingsCallCount);
        Assert.Equal(10, settingsClient.LastUpdateCafeId);
        Assert.NotNull(settingsClient.LastUpdateRequest);
        Assert.Equal("Yeni Cafe", settingsClient.LastUpdateRequest.Name);
        Assert.Equal("yeni-cafe", settingsClient.LastUpdateRequest.Slug);
        Assert.Contains("Cafe ayarları kaydedildi", postHtml, StringComparison.Ordinal);
        Assert.Contains("/c/yeni-cafe", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicationPost_ShouldBindSubmittedValueAndUseRouteCafeId()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings(isPublished: false)));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10, isPublished: false)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");
        using var getResponse = await client.GetAsync("/admin/cafes/10/settings");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "ChangeCafePublicationForm");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/settings",
            new FormUrlEncodedContent(formFields));
        var postHtml = WebUtility.HtmlDecode(await postResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(1, settingsClient.ChangePublicationCallCount);
        Assert.Equal(10, settingsClient.LastPublicationCafeId);
        Assert.NotNull(settingsClient.LastPublicationRequest);
        Assert.True(settingsClient.LastPublicationRequest.IsPublished);
        Assert.Contains("Cafe yayına alındı", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlankOptionalSlug_ShouldPostNullSlug()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");
        using var getResponse = await client.GetAsync("/admin/cafes/10/settings");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "UpdateCafeSettingsForm");
        SetFormField(formFields, "Name", "Yeni Cafe");
        SetFormField(formFields, "Slug", string.Empty);

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/settings",
            new FormUrlEncodedContent(formFields));

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(1, settingsClient.UpdateSettingsCallCount);
        Assert.NotNull(settingsClient.LastUpdateRequest);
        Assert.Null(settingsClient.LastUpdateRequest.Slug);
    }

    [Fact]
    public async Task EmptyName_ShouldShowValidationAndAvoidUpdateApiCall()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");
        using var getResponse = await client.GetAsync("/admin/cafes/10/settings");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "UpdateCafeSettingsForm");
        SetFormField(formFields, "Name", string.Empty);

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/settings",
            new FormUrlEncodedContent(formFields));
        var postHtml = WebUtility.HtmlDecode(await postResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(0, settingsClient.UpdateSettingsCallCount);
        Assert.Contains("Cafe adı zorunludur", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidSlug_ShouldShowValidationAndAvoidUpdateApiCall()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()));
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");
        using var getResponse = await client.GetAsync("/admin/cafes/10/settings");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "UpdateCafeSettingsForm");
        SetFormField(formFields, "Slug", "bad slug!");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/settings",
            new FormUrlEncodedContent(formFields));
        var postHtml = WebUtility.HtmlDecode(await postResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(0, settingsClient.UpdateSettingsCallCount);
        Assert.Contains("Menü adresi yalnız harf, rakam ve tek tire", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagerRole_ShouldRenderReadOnlyWithoutPretendingWriteAccess()
    {
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse(roleCodes: [ "CAFE_MANAGER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10, roleCodes: [ "CAFE_MANAGER" ])])),
            new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings())));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");

        using var response = await client.GetAsync("/admin/cafes/10/settings");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Salt okunur", html, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateCafeSettingsForm", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ChangeCafePublicationForm", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BackendValidationFailure_ShouldRenderSafeError()
    {
        var settingsClient = new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Success(CreateSettings()))
        {
            UpdateResult = AdminCafeSettingsRequestResult.ValidationError()
        };
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            settingsClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");
        using var getResponse = await client.GetAsync("/admin/cafes/10/settings");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "UpdateCafeSettingsForm");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/settings",
            new FormUrlEncodedContent(formFields));
        var postHtml = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Contains("Form alanlarını kontrol edin. Lütfen değerleri gözden geçirin.", WebUtility.HtmlDecode(postHtml), StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, postHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BackendFailure_ShouldRenderSafeError()
    {
        await using var factory = new AdminCafeSettingsWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/settings");

        using var response = await client.GetAsync("/admin/cafes/10/settings");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cafe ayarları yüklenemedi", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsPage_ShouldUseStaticSsrCompatibleForm()
    {
        var pageSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminCafeSettingsPage.razor"));

        Assert.Contains("[SupplyParameterFromForm(FormName = \"UpdateCafeSettingsForm\", Name = \"_formModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromForm(FormName = \"ChangeCafePublicationForm\", Name = \"_publicationActionModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"UpdateCafeSettingsForm\"", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"ChangeCafePublicationForm\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"button\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", pageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettingsPage_ShouldNotExposeCafeIdOrUnsupportedCafeFieldsAsEditableInputs()
    {
        var pageSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminCafeSettingsPage.razor"));

        Assert.DoesNotContain("name=\"CafeId\"", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"cafeId\"", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"_formModel.CafeId\"", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@bind-Value=\"_formModel.LogoImageUrl\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind-Value=\"_formModel.CoverImageUrl\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind-Value=\"_formModel.IsActive\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind-Value=\"_formModel.IsPublished\"", pageSource, StringComparison.Ordinal);
    }

    [Fact]
    public void CafeShell_ShouldLinkToCafeSettings()
    {
        var shellSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminCafeShellPage.razor"));

        Assert.Contains("/settings", shellSource, StringComparison.Ordinal);
        Assert.Contains("Cafe Ayarları", shellSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCafeSettingsApiClient_ShouldUseRealCafeEndpointsAndAuthenticatedHttpClientName()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": {
                "id": 10,
                "name": "Endpoint Cafe",
                "slug": "endpoint-cafe",
                "isActive": true,
                "isPublished": false,
                "createdAt": "2026-08-10T00:00:00+00:00",
                "updatedAt": "2026-08-10T00:00:00+00:00",
                "memberships": []
              }
            }
            """),
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": {
                "id": 10,
                "name": "Renamed Cafe",
                "slug": "renamed-cafe",
                "isActive": true,
                "isPublished": false,
                "createdAt": "2026-08-10T00:00:00+00:00",
                "updatedAt": "2026-08-10T00:00:00+00:00"
              }
            }
            """),
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": {
                "id": 10,
                "name": "Renamed Cafe",
                "slug": "renamed-cafe",
                "isActive": true,
                "isPublished": true,
                "createdAt": "2026-08-10T00:00:00+00:00",
                "updatedAt": "2026-08-10T00:00:00+00:00"
              }
            }
            """));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var httpClientFactory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminCafeSettingsApiClient(httpClientFactory);

        await apiClient.GetCafeSettingsAsync(10, CancellationToken.None);
        await apiClient.UpdateCafeSettingsAsync(
            10,
            new AdminUpdateCafeSettingsRequest("Renamed Cafe", "renamed-cafe"),
            CancellationToken.None);
        await apiClient.ChangeCafePublicationAsync(
            10,
            new AdminChangeCafePublicationRequest(true),
            CancellationToken.None);

        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, httpClientFactory.LastClientName);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://api.example.test/Cafe/GetCafeById/10", request.Uri);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Cafe/UpdateCafe/10", request.Uri);
                AssertJsonContains(request.Body, "\"name\":\"Renamed Cafe\"");
                AssertJsonContains(request.Body, "\"slug\":\"renamed-cafe\"");
                Assert.DoesNotContain("logoImageUrl", request.Body, StringComparison.Ordinal);
                Assert.DoesNotContain("coverImageUrl", request.Body, StringComparison.Ordinal);
                Assert.DoesNotContain("isActive", request.Body, StringComparison.Ordinal);
                Assert.DoesNotContain("isPublished", request.Body, StringComparison.Ordinal);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Cafe/ChangeCafePublication/10", request.Uri);
                AssertJsonContains(request.Body, "\"isPublished\":true");
                Assert.DoesNotContain("name", request.Body, StringComparison.Ordinal);
                Assert.DoesNotContain("slug", request.Body, StringComparison.Ordinal);
            });
    }

    private static async Task LoginThroughEndpointAsync(HttpClient client, string returnUrl)
    {
        using var loginResponse = await client.GetAsync($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var loginPage = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginPage);

        var antiforgeryToken = ExtractAntiforgeryToken(loginPage);
        using var response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner@example.local",
                ["Password"] = "SecurePassword123!",
                ["ReturnUrl"] = returnUrl,
                ["__RequestVerificationToken"] = antiforgeryToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
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

    private static Dictionary<string, string> ExtractFormFields(string html, string formName)
    {
        var forms = Regex.Matches(
            html,
            "<form(?<attrs>[^>]*)>(?<body>.*?)</form>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (Match form in forms)
        {
            var formHtml = form.Value;
            if (!formHtml.Contains(formName, StringComparison.Ordinal))
            {
                continue;
            }

            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            var body = form.Groups["body"].Value;
            var inputs = Regex.Matches(
                body,
                "<input(?<attrs>[^>]*)>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            foreach (Match input in inputs)
            {
                var attrs = input.Groups["attrs"].Value;
                var name = ExtractAttribute(attrs, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                fields[name] = WebUtility.HtmlDecode(ExtractAttribute(attrs, "value") ?? string.Empty);
            }

            return fields;
        }

        throw new InvalidOperationException($"Form '{formName}' was not found.");
    }

    private static void SetFormField(Dictionary<string, string> fields, string propertyName, string value)
    {
        var key = fields.Keys.FirstOrDefault(existingKey =>
            string.Equals(existingKey, propertyName, StringComparison.Ordinal) ||
            existingKey.EndsWith($".{propertyName}", StringComparison.Ordinal));

        fields[key ?? propertyName] = value;
    }

    private static void AssertFormField(Dictionary<string, string> fields, string propertyName, string? expectedValue)
    {
        var key = fields.Keys.FirstOrDefault(existingKey =>
            string.Equals(existingKey, propertyName, StringComparison.Ordinal) ||
            existingKey.EndsWith($".{propertyName}", StringComparison.Ordinal));

        Assert.NotNull(key);
        Assert.Equal(expectedValue ?? string.Empty, fields[key]);
    }

    private static string? ExtractAttribute(string attributes, string name)
    {
        var match = Regex.Match(
            attributes,
            $@"\b{name}=""(?<value>[^""]*)""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value : null;
    }

    private static AdminCafeResponse CreateCafe(
        long id = 10,
        string name = "Settings Cafe",
        string slug = "settings-cafe",
        bool isActive = true,
        bool isPublished = false,
        IReadOnlyCollection<string>? roleCodes = null)
    {
        return new AdminCafeResponse
        {
            Id = id,
            Name = name,
            Slug = slug,
            IsActive = isActive,
            IsPublished = isPublished,
            RoleCodes = roleCodes ?? [ "CAFE_OWNER" ]
        };
    }

    private static AdminCafeSettingsResponse CreateSettings(
        long id = 10,
        string name = "Settings Cafe",
        string slug = "settings-cafe",
        bool isActive = true,
        bool isPublished = false)
    {
        return new AdminCafeSettingsResponse
        {
            Id = id,
            Name = name,
            Slug = slug,
            IsActive = isActive,
            IsPublished = isPublished,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static AdminAuthResponse CreateAuthResponse(IReadOnlyCollection<string>? roleCodes = null)
    {
        return new AdminAuthResponse(
            AccessToken,
            RefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow.AddDays(7),
            new AdminUserResponse(
                10,
                "owner@example.local",
                "Cafe Owner",
                roleCodes ?? [ "CAFE_OWNER" ]));
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
                "AdminCafeSettingsPage.razor");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located.");
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static void AssertJsonContains(string? json, string expected)
    {
        Assert.NotNull(json);
        Assert.Contains(expected, json, StringComparison.Ordinal);
    }

    private sealed class FakeAdminCafeApiClient : IAdminCafeApiClient
    {
        private readonly AdminCafeListResult _result;

        public FakeAdminCafeApiClient(AdminCafeListResult result)
        {
            _result = result;
        }

        public Task<AdminCafeListResult> GetMyCafesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }

        public Task<AdminCafeDashboardStatsResult> GetCafeDashboardStatsAsync(
            long cafeId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCafeDashboardStatsResult.Failure());
        }
    }

    private sealed class FakeAdminCafeSettingsApiClient : IAdminCafeSettingsApiClient
    {
        private readonly AdminCafeSettingsRequestResult _getResult;

        public FakeAdminCafeSettingsApiClient(AdminCafeSettingsRequestResult getResult)
        {
            _getResult = getResult;
            UpdateResult = getResult;
        }

        public AdminCafeSettingsRequestResult UpdateResult { get; set; }

        public int GetSettingsCallCount { get; private set; }

        public int UpdateSettingsCallCount { get; private set; }

        public int ChangePublicationCallCount { get; private set; }

        public long? LastGetCafeId { get; private set; }

        public long? LastUpdateCafeId { get; private set; }

        public long? LastPublicationCafeId { get; private set; }

        public AdminUpdateCafeSettingsRequest? LastUpdateRequest { get; private set; }

        public AdminChangeCafePublicationRequest? LastPublicationRequest { get; private set; }

        public Task<AdminCafeSettingsRequestResult> GetCafeSettingsAsync(
            long cafeId,
            CancellationToken cancellationToken)
        {
            GetSettingsCallCount++;
            LastGetCafeId = cafeId;
            return Task.FromResult(_getResult);
        }

        public Task<AdminCafeSettingsRequestResult> UpdateCafeSettingsAsync(
            long cafeId,
            AdminUpdateCafeSettingsRequest request,
            CancellationToken cancellationToken)
        {
            UpdateSettingsCallCount++;
            LastUpdateCafeId = cafeId;
            LastUpdateRequest = request;

            if (UpdateResult.Status == AdminCafeSettingsRequestStatus.Success)
            {
                return Task.FromResult(AdminCafeSettingsRequestResult.Success(CreateSettings(
                    id: cafeId,
                    name: request.Name,
                    slug: request.Slug ?? "settings-cafe")));
            }

            return Task.FromResult(UpdateResult);
        }

        public Task<AdminCafeSettingsRequestResult> ChangeCafePublicationAsync(
            long cafeId,
            AdminChangeCafePublicationRequest request,
            CancellationToken cancellationToken)
        {
            ChangePublicationCallCount++;
            LastPublicationCafeId = cafeId;
            LastPublicationRequest = request;

            return Task.FromResult(AdminCafeSettingsRequestResult.Success(CreateSettings(
                id: cafeId,
                isPublished: request.IsPublished)));
        }
    }

    private sealed class FakeAdminAuthApiClient : IAdminAuthApiClient
    {
        private readonly AdminAuthResponse? _loginResponse;

        public FakeAdminAuthApiClient(AdminAuthResponse? loginResponse = null)
        {
            _loginResponse = loginResponse;
        }

        public Task<AdminAuthResponse?> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResponse);
        }

        public Task<AdminAuthResponse?> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminAuthResponse?>(null);
        }

        public Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class AdminCafeSettingsWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;
        private readonly IAdminCafeSettingsApiClient _adminCafeSettingsApiClient;

        public AdminCafeSettingsWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient,
            IAdminCafeSettingsApiClient adminCafeSettingsApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
            _adminCafeSettingsApiClient = adminCafeSettingsApiClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthApiClient>();
                services.AddSingleton(_authApiClient);
                services.RemoveAll<IAdminCafeApiClient>();
                services.AddSingleton(_adminCafeApiClient);
                services.RemoveAll<IAdminCafeSettingsApiClient>();
                services.AddSingleton(_adminCafeSettingsApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-cafe-settings-test-data-protection"));
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

        public Task<PublicProductDetailRequestResult> GetProductDetailAsync(
            string slug,
            long productId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PublicProductDetailRequestResult.NotFound());
        }
    }

    private sealed class RecordingHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public RecordingHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string? LastClientName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            LastClientName = name;
            return _httpClient;
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string? Uri, string? Body);

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.ToString(), body));

            return _responses.Dequeue();
        }
    }
}
