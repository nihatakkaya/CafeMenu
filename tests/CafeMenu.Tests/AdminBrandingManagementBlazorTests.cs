extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminBranding;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
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

public sealed class AdminBrandingManagementBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public async Task BrandingAdminRoute_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin/cafes/10/branding");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/account/login?returnUrl=%2Fadmin%2Fcafes%2F10%2Fbranding",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AccessibleCafe_ShouldOpenBrandingPageAndLoadBranding()
    {
        var brandingClient = new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding()));
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            brandingClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");

        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, brandingClient.GetBrandingCallCount);
        Assert.Equal(10, brandingClient.LastCafeId);
        Assert.Contains("Branding Management Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Görünüm formu", html, StringComparison.Ordinal);
        Assert.Contains("/c/branding-management-cafe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InaccessibleCafe_ShouldRejectBeforeBrandingApiCall()
    {
        var brandingClient = new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding()));
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            brandingClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/branding");

        using var response = await client.GetAsync("/admin/cafes/99/branding");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Equal(0, brandingClient.GetBrandingCallCount);
        Assert.Equal(0, brandingClient.UpdateBrandingCallCount);
    }

    [Fact]
    public async Task DefaultTheme_ShouldRenderAsLoadedDefaultState()
    {
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateDefaultBranding())));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");

        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Varsayılan tema değerleri gösteriliyor", html, StringComparison.Ordinal);
        Assert.Contains(AdminBrandingConstants.ClassicThemePreset, html, StringComparison.Ordinal);
        Assert.Contains(AdminBrandingConstants.SystemFontPreset, html, StringComparison.Ordinal);
        Assert.Contains(AdminBrandingConstants.DefaultPrimaryColor, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExistingThemeValues_ShouldBindToForm()
    {
        var branding = CreateBranding(
            logoImageUrl: "https://cdn.example.test/logo.png",
            coverImageUrl: "https://cdn.example.test/cover.png",
            primaryColor: "#1A1A1A",
            secondaryColor: "#F5F5F5",
            accentColor: "#D97706",
            backgroundColor: "#FFFFFF",
            textColor: "#111111",
            welcomeTitle: "Hoş geldiniz",
            welcomeDescription: "Günlük menü",
            fontPreset: AdminBrandingConstants.SerifFontPreset,
            themePreset: AdminBrandingConstants.ModernThemePreset,
            isPublished: true);
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(branding)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");

        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = await response.Content.ReadAsStringAsync();
        var fields = ExtractFormFields(html, "UpdateCafeBrandingForm");

        AssertFormField(fields, "LogoImageUrl", branding.LogoImageUrl);
        AssertFormField(fields, "CoverImageUrl", branding.CoverImageUrl);
        AssertFormField(fields, "PrimaryColor", branding.PrimaryColor);
        AssertFormField(fields, "SecondaryColor", branding.SecondaryColor);
        AssertFormField(fields, "AccentColor", branding.AccentColor);
        AssertFormField(fields, "BackgroundColor", branding.BackgroundColor);
        AssertFormField(fields, "TextColor", branding.TextColor);
        AssertFormField(fields, "WelcomeTitle", branding.WelcomeTitle);
        AssertFormField(fields, "WelcomeDescription", branding.WelcomeDescription);
        AssertFormField(fields, "FontPreset", branding.FontPreset);
        AssertFormField(fields, "ThemePreset", branding.ThemePreset);
        AssertFormField(fields, "IsPublished", "True");
    }

    [Fact]
    public async Task BrandingForm_ShouldUseControlledPresetSelections()
    {
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding())));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");

        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("<select", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"_formModel.ThemePreset\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"CLASSIC\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"MODERN\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"COMPACT\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"_formModel.FontPreset\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"SYSTEM\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"SANS\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"SERIF\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"_formModel.ThemePreset\" type=\"text\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"_formModel.FontPreset\" type=\"text\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SavePost_ShouldBindSubmittedValuesAndUseRouteCafeId()
    {
        var brandingClient = new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding()));
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            brandingClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");
        using var getResponse = await client.GetAsync("/admin/cafes/10/branding");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var formFields = ExtractFormFields(getHtml, "UpdateCafeBrandingForm");
        SetFormField(formFields, "LogoImageUrl", "https://cdn.example.test/new-logo.png");
        SetFormField(formFields, "CoverImageUrl", string.Empty);
        SetFormField(formFields, "PrimaryColor", "#222222");
        SetFormField(formFields, "SecondaryColor", "#EEEEEE");
        SetFormField(formFields, "AccentColor", "#AA5500");
        SetFormField(formFields, "BackgroundColor", "#FFFFFF");
        SetFormField(formFields, "TextColor", "#111111");
        SetFormField(formFields, "WelcomeTitle", "Yeni başlık");
        SetFormField(formFields, "WelcomeDescription", "Yeni açıklama");
        SetFormField(formFields, "FontPreset", AdminBrandingConstants.SansFontPreset);
        SetFormField(formFields, "ThemePreset", AdminBrandingConstants.CompactThemePreset);
        SetFormField(formFields, "IsPublished", "true");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/branding",
            new FormUrlEncodedContent(formFields));
        var postHtml = WebUtility.HtmlDecode(await postResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(1, brandingClient.UpdateBrandingCallCount);
        Assert.Equal(10, brandingClient.LastUpdateCafeId);
        Assert.NotNull(brandingClient.LastUpdateRequest);
        Assert.Equal("https://cdn.example.test/new-logo.png", brandingClient.LastUpdateRequest.LogoImageUrl);
        Assert.Null(brandingClient.LastUpdateRequest.CoverImageUrl);
        Assert.Equal("#222222", brandingClient.LastUpdateRequest.PrimaryColor);
        Assert.Equal("#EEEEEE", brandingClient.LastUpdateRequest.SecondaryColor);
        Assert.Equal("#AA5500", brandingClient.LastUpdateRequest.AccentColor);
        Assert.Equal("#FFFFFF", brandingClient.LastUpdateRequest.BackgroundColor);
        Assert.Equal("#111111", brandingClient.LastUpdateRequest.TextColor);
        Assert.Equal("Yeni başlık", brandingClient.LastUpdateRequest.WelcomeTitle);
        Assert.Equal("Yeni açıklama", brandingClient.LastUpdateRequest.WelcomeDescription);
        Assert.Equal(AdminBrandingConstants.SansFontPreset, brandingClient.LastUpdateRequest.FontPreset);
        Assert.Equal(AdminBrandingConstants.CompactThemePreset, brandingClient.LastUpdateRequest.ThemePreset);
        Assert.True(brandingClient.LastUpdateRequest.IsPublished);
        Assert.Contains("Görünüm ayarları kaydedildi", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BrandingPage_ShouldNotExposeCafeIdAsUserEditableFormInput()
    {
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding())));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");

        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("name=\"CafeId\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"cafeId\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"_formModel.CafeId\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnsafeInputValidation_ShouldBlockPostBeforeApiCall()
    {
        var brandingClient = new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding()));
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            brandingClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");
        using var getResponse = await client.GetAsync("/admin/cafes/10/branding");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var formFields = ExtractFormFields(getHtml, "UpdateCafeBrandingForm");
        SetFormField(formFields, "WelcomeTitle", "<script>alert(1)</script>");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/branding",
            new FormUrlEncodedContent(formFields));
        var postHtml = WebUtility.HtmlDecode(await postResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(0, brandingClient.UpdateBrandingCallCount);
        Assert.Contains("HTML, CSS veya JavaScript", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BackendValidationFailure_ShouldRenderSafeError()
    {
        var brandingClient = new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Success(CreateBranding()))
        {
            UpdateResult = AdminBrandingRequestResult.ValidationError()
        };
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            brandingClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");
        using var getResponse = await client.GetAsync("/admin/cafes/10/branding");
        var formFields = ExtractFormFields(await getResponse.Content.ReadAsStringAsync(), "UpdateCafeBrandingForm");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/branding",
            new FormUrlEncodedContent(formFields));
        var postHtml = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Contains("Backend doğrulaması isteği reddetti", WebUtility.HtmlDecode(postHtml), StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, postHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BackendFailure_ShouldRenderSafeError()
    {
        await using var factory = new AdminBrandingWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminBrandingApiClient(AdminBrandingRequestResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/branding");

        using var response = await client.GetAsync("/admin/cafes/10/branding");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Görünüm ayarları yüklenemedi", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public void BrandingPage_ShouldUseStaticSsrCompatibleForm()
    {
        var pageSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminBrandingManagementPage.razor"));

        Assert.Contains("[SupplyParameterFromForm(FormName = \"UpdateCafeBrandingForm\", Name = \"_formModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"UpdateCafeBrandingForm\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"button\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("custom css", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", pageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CafeShell_ShouldLinkToBrandingManagement()
    {
        var shellSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminCafeShellPage.razor"));

        Assert.Contains("/branding", shellSource, StringComparison.Ordinal);
        Assert.Contains("Görünüm", shellSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminBrandingApiClient_ShouldUseAuthenticatedAdminHttpClientAndBackendRoutes()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": {
                "cafeId": 10,
                "cafeName": "Cafe",
                "logoImageUrl": null,
                "coverImageUrl": null,
                "primaryColor": "#111827",
                "secondaryColor": "#F9FAFB",
                "accentColor": "#D97706",
                "backgroundColor": "#FFFFFF",
                "textColor": "#111827",
                "welcomeTitle": null,
                "welcomeDescription": null,
                "fontPreset": "SYSTEM",
                "themePreset": "CLASSIC",
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
                "cafeId": 10,
                "cafeName": "Cafe",
                "logoImageUrl": "https://cdn.example.test/logo.png",
                "coverImageUrl": null,
                "primaryColor": "#222222",
                "secondaryColor": "#EEEEEE",
                "accentColor": "#AA5500",
                "backgroundColor": "#FFFFFF",
                "textColor": "#111111",
                "welcomeTitle": "Title",
                "welcomeDescription": "Description",
                "fontPreset": "SANS",
                "themePreset": "MODERN",
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
        var apiClient = new AdminBrandingApiClient(httpClientFactory);

        await apiClient.GetCafeBrandingAsync(10, CancellationToken.None);
        await apiClient.UpdateCafeBrandingAsync(
            10,
            new AdminUpdateCafeBrandingRequest(
                "https://cdn.example.test/logo.png",
                null,
                "#222222",
                "#EEEEEE",
                "#AA5500",
                "#FFFFFF",
                "#111111",
                "Title",
                "Description",
                "SANS",
                "MODERN",
                true),
            CancellationToken.None);

        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, httpClientFactory.LastClientName);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://api.example.test/CafeBranding/GetCafeBranding/10", request.Uri);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/CafeBranding/UpdateCafeBranding/10", request.Uri);
                AssertJsonContains(request.Body, "\"logoImageUrl\":\"https://cdn.example.test/logo.png\"");
                AssertJsonContains(request.Body, "\"primaryColor\":\"#222222\"");
                AssertJsonContains(request.Body, "\"fontPreset\":\"SANS\"");
                AssertJsonContains(request.Body, "\"themePreset\":\"MODERN\"");
                AssertJsonContains(request.Body, "\"isPublished\":true");
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

            var textareas = Regex.Matches(
                body,
                "<textarea(?<attrs>[^>]*)>(?<value>.*?)</textarea>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            foreach (Match textarea in textareas)
            {
                var attrs = textarea.Groups["attrs"].Value;
                var name = ExtractAttribute(attrs, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                fields[name] = WebUtility.HtmlDecode(textarea.Groups["value"].Value);
            }

            var selects = Regex.Matches(
                body,
                "<select(?<attrs>[^>]*)>(?<options>.*?)</select>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            foreach (Match select in selects)
            {
                var attrs = select.Groups["attrs"].Value;
                var name = ExtractAttribute(attrs, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var options = Regex.Matches(
                    select.Groups["options"].Value,
                    "<option(?<attrs>[^>]*)>",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var selected = options.Cast<Match>().FirstOrDefault(option =>
                    option.Groups["attrs"].Value.Contains("selected", StringComparison.OrdinalIgnoreCase));
                selected ??= options.Cast<Match>().FirstOrDefault();

                if (selected is not null)
                {
                    fields[name] = WebUtility.HtmlDecode(ExtractAttribute(selected.Groups["attrs"].Value, "value") ?? string.Empty);
                }
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

    private static AdminCafeResponse CreateCafe(long id)
    {
        return new AdminCafeResponse
        {
            Id = id,
            Name = "Branding Management Cafe",
            Slug = "branding-management-cafe",
            IsActive = true,
            IsPublished = false,
            RoleCodes = [ "CAFE_OWNER" ]
        };
    }

    private static AdminBrandingResponse CreateDefaultBranding()
    {
        return CreateBranding(
            primaryColor: AdminBrandingConstants.DefaultPrimaryColor,
            secondaryColor: AdminBrandingConstants.DefaultSecondaryColor,
            accentColor: AdminBrandingConstants.DefaultAccentColor,
            backgroundColor: AdminBrandingConstants.DefaultBackgroundColor,
            textColor: AdminBrandingConstants.DefaultTextColor,
            welcomeTitle: null,
            welcomeDescription: null,
            fontPreset: AdminBrandingConstants.SystemFontPreset,
            themePreset: AdminBrandingConstants.ClassicThemePreset,
            isPublished: false);
    }

    private static AdminBrandingResponse CreateBranding(
        long cafeId = 10,
        string cafeName = "Branding Management Cafe",
        string? logoImageUrl = null,
        string? coverImageUrl = null,
        string primaryColor = "#111827",
        string secondaryColor = "#F9FAFB",
        string accentColor = "#D97706",
        string backgroundColor = "#FFFFFF",
        string textColor = "#111827",
        string? welcomeTitle = "Welcome",
        string? welcomeDescription = "Fresh menu selections",
        string fontPreset = "SYSTEM",
        string themePreset = "CLASSIC",
        bool isPublished = false)
    {
        return new AdminBrandingResponse
        {
            CafeId = cafeId,
            CafeName = cafeName,
            LogoImageUrl = logoImageUrl,
            CoverImageUrl = coverImageUrl,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            AccentColor = accentColor,
            BackgroundColor = backgroundColor,
            TextColor = textColor,
            WelcomeTitle = welcomeTitle,
            WelcomeDescription = welcomeDescription,
            FontPreset = fontPreset,
            ThemePreset = themePreset,
            IsPublished = isPublished,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static AdminAuthResponse CreateAuthResponse()
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
                [ "CAFE_OWNER" ]));
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
                "AdminBrandingManagementPage.razor");

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
    }

    private sealed class FakeAdminBrandingApiClient : IAdminBrandingApiClient
    {
        private readonly AdminBrandingRequestResult _getResult;

        public FakeAdminBrandingApiClient(AdminBrandingRequestResult getResult)
        {
            _getResult = getResult;
            UpdateResult = getResult;
        }

        public AdminBrandingRequestResult UpdateResult { get; set; }

        public int GetBrandingCallCount { get; private set; }

        public int UpdateBrandingCallCount { get; private set; }

        public long? LastCafeId { get; private set; }

        public long? LastUpdateCafeId { get; private set; }

        public AdminUpdateCafeBrandingRequest? LastUpdateRequest { get; private set; }

        public Task<AdminBrandingRequestResult> GetCafeBrandingAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetBrandingCallCount++;
            LastCafeId = cafeId;
            return Task.FromResult(_getResult);
        }

        public Task<AdminBrandingRequestResult> UpdateCafeBrandingAsync(
            long cafeId,
            AdminUpdateCafeBrandingRequest request,
            CancellationToken cancellationToken)
        {
            UpdateBrandingCallCount++;
            LastUpdateCafeId = cafeId;
            LastUpdateRequest = request;

            if (UpdateResult.Status == AdminBrandingRequestStatus.Success)
            {
                return Task.FromResult(AdminBrandingRequestResult.Success(CreateBranding(
                    cafeId: cafeId,
                    logoImageUrl: request.LogoImageUrl,
                    coverImageUrl: request.CoverImageUrl,
                    primaryColor: request.PrimaryColor,
                    secondaryColor: request.SecondaryColor,
                    accentColor: request.AccentColor,
                    backgroundColor: request.BackgroundColor,
                    textColor: request.TextColor,
                    welcomeTitle: request.WelcomeTitle,
                    welcomeDescription: request.WelcomeDescription,
                    fontPreset: request.FontPreset,
                    themePreset: request.ThemePreset,
                    isPublished: request.IsPublished)));
            }

            return Task.FromResult(UpdateResult);
        }
    }

    private sealed class FakeAdminAuthApiClient : IAdminAuthApiClient
    {
        private readonly AdminAuthResponse? _loginResponse;

        public FakeAdminAuthApiClient(AdminAuthResponse? loginResponse = null)
        {
            _loginResponse = loginResponse;
        }

        public Task<AdminAuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(_loginResponse);
        }

        public Task<AdminAuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<AdminAuthResponse?>(null);
        }

        public Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class AdminBrandingWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;
        private readonly IAdminBrandingApiClient _adminBrandingApiClient;

        public AdminBrandingWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient,
            IAdminBrandingApiClient adminBrandingApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
            _adminBrandingApiClient = adminBrandingApiClient;
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
                services.RemoveAll<IAdminBrandingApiClient>();
                services.AddSingleton(_adminBrandingApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-branding-test-data-protection"));
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
