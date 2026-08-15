extern alias CafeMenuWeb;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminPlatform;
using CafeMenuWeb::CafeMenu.Web.PublicMenu;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WebProgram = CafeMenuWeb::Program;

namespace CafeMenu.Tests;

public sealed class AdminPlatformOnboardingBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";
    private const string SetupToken = "setup-token-visible-once";

    [Fact]
    public async Task PlatformPage_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            new FakeAdminPlatformApiClient());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin/platform");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login?returnUrl=%2Fadmin%2Fplatform", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PlatformPage_ShouldDenyNonPlatformUser()
    {
        var platformClient = new FakeAdminPlatformApiClient();
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateAdminCafe()])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform");

        using var response = await client.GetAsync("/admin/platform");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Platform yönetimi", html, StringComparison.Ordinal);
        Assert.Equal(0, platformClient.GetCafesCallCount);
    }

    [Fact]
    public async Task PlatformPage_ShouldRenderCafesAndStaticSsrFormsForPlatformAdmin()
    {
        var platformClient = new FakeAdminPlatformApiClient(
            cafes: [CreatePlatformCafe(isActive: false)],
            stats: CreatePlatformStats());
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform");

        using var response = await client.GetAsync("/admin/platform");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Platform yönetimi", html, StringComparison.Ordinal);
        Assert.Contains("Mocca Platform", html, StringComparison.Ordinal);
        Assert.Contains("mocca-platform", html, StringComparison.Ordinal);
        Assert.Contains("Pasif", html, StringComparison.Ordinal);
        Assert.Contains("Taslak", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/50\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/platform/cafes/50/members\"", html, StringComparison.Ordinal);
        Assert.Contains("Aktif cafe", html, StringComparison.Ordinal);
        Assert.Contains(">4<", html, StringComparison.Ordinal);
        Assert.Contains("Pasif cafe", html, StringComparison.Ordinal);
        Assert.Contains(">1<", html, StringComparison.Ordinal);
        Assert.Contains("Yayındaki cafe", html, StringComparison.Ordinal);
        Assert.Contains(">3<", html, StringComparison.Ordinal);
        Assert.Contains("Taslak cafe", html, StringComparison.Ordinal);
        Assert.Contains(">2<", html, StringComparison.Ordinal);
        Assert.Contains("CreatePlatformCafeForm", html, StringComparison.Ordinal);
        Assert.Contains("PlatformCafeActionForm", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
        AssertFormContainsTokenAndSubmitAction(html, "CreatePlatformCafeForm", "__RequestVerificationToken", "Cafe oluştur");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformCafeActionForm", "Activate|50", "Aktif yap");
        Assert.Contains("class=\"nav-link active\" href=\"admin/platform\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"nav-link active\" href=\"admin\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformPage_CreateCafeForm_ShouldPostBoundValuesToApiClient()
    {
        var platformClient = new FakeAdminPlatformApiClient(cafes: []);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform");
        var page = await client.GetStringAsync("/admin/platform");
        var form = ExtractFormFields(page, "CreatePlatformCafeForm");
        form["_createCafeModel.Name"] = "New Platform Cafe";
        form["_createCafeModel.Slug"] = "new-platform-cafe";

        using var response = await client.PostAsync("/admin/platform", new FormUrlEncodedContent(form));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("New Platform Cafe", platformClient.LastCreateCafeRequest?.Name);
        Assert.Equal("new-platform-cafe", platformClient.LastCreateCafeRequest?.Slug);
        Assert.Contains("Cafe oluşturuldu", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/77\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformPage_CafeActivateDeactivateActions_ShouldUseStaticSsrActionForm()
    {
        var platformClient = new FakeAdminPlatformApiClient(cafes: [CreatePlatformCafe(isActive: false)]);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform");
        var page = await client.GetStringAsync("/admin/platform");
        var form = ExtractFormFields(page, "PlatformCafeActionForm");
        form["_cafeActionModel.Action"] = "Activate|50";

        using var activateResponse = await client.PostAsync("/admin/platform", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        Assert.Equal(50, platformClient.LastActivatedCafeId);

        page = await client.GetStringAsync("/admin/platform");
        form = ExtractFormFields(page, "PlatformCafeActionForm");
        form["_cafeActionModel.Action"] = "Deactivate|50";

        using var deactivateResponse = await client.PostAsync("/admin/platform", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.Equal(50, platformClient.LastDeactivatedCafeId);
    }

    [Fact]
    public async Task MembersPage_ShouldUseRouteCafeIdAndRenderMinimalMemberData()
    {
        var platformClient = new FakeAdminPlatformApiClient(
            cafes: [CreatePlatformCafe()],
            members: [CreateMember()]);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform/cafes/50/members");

        using var response = await client.GetAsync("/admin/platform/cafes/50/members");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(50, platformClient.LastGetMembersCafeId);
        Assert.Contains("Owner User", html, StringComparison.Ordinal);
        Assert.Contains("owner@example.local", html, StringComparison.Ordinal);
        Assert.Contains("Cafe Sahibi", html, StringComparison.Ordinal);
        Assert.Contains("Aktif", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Kullanıcı no", html, StringComparison.Ordinal);
        Assert.DoesNotContain("CAFE_OWNER", html, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", html, StringComparison.OrdinalIgnoreCase);
        AssertFormContainsSingleToken(html, "PlatformUserSearchForm");
        AssertFormContainsSingleToken(html, "PlatformUserSearchActionForm");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformMemberActionForm", "Reissue|120", "Yeni kod oluştur");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformMemberActionForm", "AssignOwner|120", "Sahip olarak ata");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformMemberActionForm", "AssignManager|120", "Yönetici olarak ata");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformMemberActionForm", "Deactivate|900", "Pasif yap");
    }

    [Fact]
    public async Task MembersPage_CreateUserSetup_ShouldRenderTokenOnceWithoutBrowserStorage()
    {
        var platformClient = new FakeAdminPlatformApiClient(cafes: [CreatePlatformCafe()]);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform/cafes/50/members");
        var page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        var form = ExtractFormFields(page, "CreatePlatformUserSetupForm");
        form["_createUserSetupModel.Email"] = "new.owner@example.local";
        form["_createUserSetupModel.FullName"] = "New Owner";

        using var response = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(form));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("new.owner@example.local", platformClient.LastCreateUserSetupRequest?.Email);
        Assert.Equal("New Owner", platformClient.LastCreateUserSetupRequest?.FullName);
        Assert.Contains(SetupToken, html, StringComparison.Ordinal);
        Assert.Contains("yalnızca bu başarılı işlemden sonra gösterilir", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/account/setup\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Kullanıcı no", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"/account/setup?token={SetupToken}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", html, StringComparison.OrdinalIgnoreCase);
        AssertFormContainsTokenAndSubmitAction(html, "PlatformSetupActionForm", "Reissue|120", "Yeni kod oluştur");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformSetupActionForm", "AssignOwner|120", "Sahip olarak ata");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformSetupActionForm", "AssignManager|120", "Yönetici olarak ata");

        using var refreshResponse = await client.GetAsync("/admin/platform/cafes/50/members");
        var refreshHtml = await refreshResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SetupToken, refreshHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MembersPage_UserSearch_ShouldRenderAssignableActiveUsersWithAntiforgery()
    {
        var platformClient = new FakeAdminPlatformApiClient(
            cafes: [CreatePlatformCafe()],
            searchedUsers: [CreateSearchUser()]);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform/cafes/50/members");
        var page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        AssertFormContainsTokenAndSubmitAction(page, "PlatformUserSearchForm", "__RequestVerificationToken", "Kullanıcı ara");

        var form = ExtractFormFields(page, "PlatformUserSearchForm");
        form["_userSearchModel.Query"] = "found owner";

        using var response = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(form));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new AdminPlatformUserSearchRequest("found owner"), platformClient.LastSearchUsersRequest);
        Assert.Equal(1, platformClient.SearchUsersCallCount);
        Assert.Contains("Searchable Owner", html, StringComparison.Ordinal);
        Assert.Contains("searchable.owner@example.local", html, StringComparison.Ordinal);
        Assert.Contains("Aktif", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Kullanıcı no", html, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SetupToken", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", html, StringComparison.OrdinalIgnoreCase);
        AssertFormContainsTokenAndSubmitAction(html, "PlatformUserSearchActionForm", "AssignOwner|444", "Sahip olarak ata");
        AssertFormContainsTokenAndSubmitAction(html, "PlatformUserSearchActionForm", "AssignManager|444", "Yönetici olarak ata");
    }

    [Fact]
    public async Task MembersPage_UserSearchActions_ShouldUseSemanticAssignmentWithoutRoleFields()
    {
        var platformClient = new FakeAdminPlatformApiClient(
            cafes: [CreatePlatformCafe()],
            searchedUsers: [CreateSearchUser()]);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform/cafes/50/members");
        var page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        var searchForm = ExtractFormFields(page, "PlatformUserSearchForm");
        searchForm["_userSearchModel.Query"] = "searchable";

        using var searchResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(searchForm));
        page = await searchResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("RoleId", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RoleCode", page, StringComparison.OrdinalIgnoreCase);

        var actionForm = ExtractFormFields(page, "PlatformUserSearchActionForm");
        actionForm["_userSearchActionModel.Action"] = "AssignOwner|444";
        using var ownerResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(actionForm));
        Assert.True(
            ownerResponse.StatusCode == HttpStatusCode.OK,
            await ownerResponse.Content.ReadAsStringAsync());
        Assert.Equal(new AdminPlatformAssignCafeMemberRequest(50, 444), platformClient.LastAssignOwnerRequest);

        page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        searchForm = ExtractFormFields(page, "PlatformUserSearchForm");
        searchForm["_userSearchModel.Query"] = "searchable";
        using var secondSearchResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(searchForm));
        page = await secondSearchResponse.Content.ReadAsStringAsync();

        actionForm = ExtractFormFields(page, "PlatformUserSearchActionForm");
        actionForm["_userSearchActionModel.Action"] = "AssignManager|444";
        using var managerResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(actionForm));
        Assert.True(
            managerResponse.StatusCode == HttpStatusCode.OK,
            await managerResponse.Content.ReadAsStringAsync());
        Assert.Equal(new AdminPlatformAssignCafeMemberRequest(50, 444), platformClient.LastAssignManagerRequest);
    }

    [Fact]
    public async Task MembersPage_Actions_ShouldUseSemanticEndpointsWithoutRoleFields()
    {
        var platformClient = new FakeAdminPlatformApiClient(
            cafes: [CreatePlatformCafe()],
            members: [CreateMember()]);
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "PLATFORM_ADMIN" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            platformClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/platform/cafes/50/members");
        var page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        Assert.DoesNotContain("RoleId", page, StringComparison.OrdinalIgnoreCase);

        var form = ExtractFormFields(page, "PlatformMemberActionForm");
        form["_memberActionModel.Action"] = "AssignOwner|120";
        using var ownerResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(new AdminPlatformAssignCafeMemberRequest(50, 120), platformClient.LastAssignOwnerRequest);

        page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        form = ExtractFormFields(page, "PlatformMemberActionForm");
        form["_memberActionModel.Action"] = "AssignManager|120";
        using var managerResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
        Assert.Equal(new AdminPlatformAssignCafeMemberRequest(50, 120), platformClient.LastAssignManagerRequest);

        page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        form = ExtractFormFields(page, "PlatformMemberActionForm");
        form["_memberActionModel.Action"] = "Reissue|120";
        using var reissueResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, reissueResponse.StatusCode);
        Assert.Equal(120, platformClient.LastReissueUserId);

        page = await client.GetStringAsync("/admin/platform/cafes/50/members");
        form = ExtractFormFields(page, "PlatformMemberActionForm");
        form["_memberActionModel.Action"] = "Deactivate|900";
        using var deactivateResponse = await client.PostAsync("/admin/platform/cafes/50/members", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.Equal(900, platformClient.LastDeactivatedMembershipId);
    }

    [Fact]
    public async Task ExistingAdminPage_ShouldRemainAvailableForOwner()
    {
        await using var factory = new AdminPlatformWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse([ "CAFE_OWNER" ])),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateAdminCafe()])),
            new FakeAdminPlatformApiClient());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin");

        using var response = await client.GetAsync("/admin");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Owner Cafe", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/cafes/10\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminPlatformApiClient_ShouldUseAuthenticatedClientAndSemanticEndpoints()
    {
        var handler = new RecordingHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, """
                {
                  "success": true,
                  "message": "ok",
                  "data": []
                }
                """),
            JsonResponse(HttpStatusCode.OK, """
                {
                  "success": true,
                  "message": "ok",
                  "data": [
                    {
                      "appUserId": 444,
                      "email": "searchable.owner@example.local",
                      "fullName": "Searchable Owner",
                      "isActive": true
                    }
                  ]
                }
                """),
            JsonResponse(HttpStatusCode.OK, """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "id": 9,
                    "cafeId": 50,
                    "appUserId": 120,
                    "userEmail": "owner@example.local",
                    "userFullName": "Owner User",
                    "roleCode": "CAFE_OWNER",
                    "isActive": true
                  }
                }
                """)
        ]);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var factory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminPlatformApiClient(factory);

        await apiClient.GetCafesAsync(CancellationToken.None);
        await apiClient.SearchUsersAsync(new AdminPlatformUserSearchRequest("owner query"), CancellationToken.None);
        await apiClient.AssignCafeOwnerAsync(new AdminPlatformAssignCafeMemberRequest(50, 120), CancellationToken.None);

        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, factory.LastClientName);
        Assert.Equal("https://api.example.test/Cafe/GetCafes", handler.Requests[0].RequestUri?.ToString());
        Assert.Equal("https://api.example.test/PlatformUser/SearchUsers?query=owner query&pageSize=10", handler.Requests[1].RequestUri?.ToString());
        Assert.Equal("https://api.example.test/Cafe/AssignCafeOwner", handler.Requests[2].RequestUri?.ToString());
        Assert.DoesNotContain("roleId", handler.RequestBodies[2], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("roleCode", handler.RequestBodies[2], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"cafeId\":50", handler.RequestBodies[2], StringComparison.Ordinal);
        Assert.Contains("\"appUserId\":120", handler.RequestBodies[2], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminPlatformApiClient_ShouldCallPlatformDashboardStatsEndpoint()
    {
        var handler = new RecordingHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "activeCafeCount": 4,
                    "inactiveCafeCount": 1,
                    "publishedCafeCount": 3,
                    "draftCafeCount": 2
                  }
                }
                """)
        ]);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var factory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminPlatformApiClient(factory);

        var result = await apiClient.GetPlatformDashboardStatsAsync(CancellationToken.None);

        Assert.Equal(AdminPlatformRequestStatus.Success, result.Status);
        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, factory.LastClientName);
        Assert.Equal("https://api.example.test/Cafe/GetPlatformDashboardStats", handler.Requests[0].RequestUri?.ToString());
        Assert.Equal(4, result.Stats?.ActiveCafeCount);
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
                ["Email"] = "admin@example.local",
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

    private static Dictionary<string, string> ExtractFormFields(string html, string formId)
    {
        var formMatch = Regex.Match(
            html,
            $@"<form(?=[^>]*id=""{Regex.Escape(formId)}"")[\s\S]*?</form>",
            RegexOptions.CultureInvariant);

        Assert.True(formMatch.Success, html);

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match inputMatch in Regex.Matches(formMatch.Value, "<input[^>]*>", RegexOptions.CultureInvariant))
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

    private static void AssertFormContainsTokenAndSubmitAction(
        string html,
        string formId,
        string expectedValue,
        string expectedLabel)
    {
        var formHtml = ExtractFormHtml(html, formId);

        Assert.Equal(1, CountAntiforgeryTokenInputs(formHtml));
        Assert.Contains(expectedValue, WebUtility.HtmlDecode(formHtml), StringComparison.Ordinal);
        Assert.Contains(expectedLabel, WebUtility.HtmlDecode(formHtml), StringComparison.Ordinal);
        Assert.DoesNotContain($"form=\"{formId}\"", formHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertFormContainsSingleToken(string html, string formId)
    {
        var formHtml = ExtractFormHtml(html, formId);

        Assert.Equal(1, CountAntiforgeryTokenInputs(formHtml));
    }

    private static string? GetAttributeValue(string tag, string attributeName)
    {
        var match = Regex.Match(
            tag,
            $@"\s{Regex.Escape(attributeName)}=""(?<value>[^""]*)""",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["value"].Value : null;
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

    private static int CountAntiforgeryTokenInputs(string formHtml)
    {
        return Regex.Matches(
            formHtml,
            @"<input(?=[^>]*name=""__RequestVerificationToken"")[^>]*>",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Count;
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static AdminCafeResponse CreateAdminCafe()
    {
        return new AdminCafeResponse
        {
            Id = 10,
            Name = "Owner Cafe",
            Slug = "owner-cafe",
            IsActive = true,
            IsPublished = true,
            RoleCodes = [ "CAFE_OWNER" ]
        };
    }

    private static AdminPlatformCafeResponse CreatePlatformCafe(bool isActive = true)
    {
        return new AdminPlatformCafeResponse
        {
            Id = 50,
            Name = "Mocca Platform",
            Slug = "mocca-platform",
            IsActive = isActive,
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static AdminPlatformDashboardStatsResponse CreatePlatformStats()
    {
        return new AdminPlatformDashboardStatsResponse
        {
            ActiveCafeCount = 4,
            InactiveCafeCount = 1,
            PublishedCafeCount = 3,
            DraftCafeCount = 2
        };
    }

    private static AdminPlatformCafeMemberResponse CreateMember()
    {
        return new AdminPlatformCafeMemberResponse
        {
            MembershipId = 900,
            AppUserId = 120,
            Email = "owner@example.local",
            FullName = "Owner User",
            RoleCode = "CAFE_OWNER",
            IsActive = true
        };
    }

    private static AdminPlatformUserSearchResponse CreateSearchUser()
    {
        return new AdminPlatformUserSearchResponse
        {
            AppUserId = 444,
            Email = "searchable.owner@example.local",
            FullName = "Searchable Owner",
            IsActive = true
        };
    }

    private static AdminAuthResponse CreateAuthResponse(IReadOnlyCollection<string> roles)
    {
        return new AdminAuthResponse(
            AccessToken,
            RefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow.AddDays(7),
            new AdminUserResponse(
                10,
                "admin@example.local",
                "Admin User",
                roles));
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

    private sealed class FakeAdminPlatformApiClient : IAdminPlatformApiClient
    {
        private readonly IReadOnlyCollection<AdminPlatformCafeResponse> _cafes;
        private readonly IReadOnlyCollection<AdminPlatformCafeMemberResponse> _members;
        private readonly IReadOnlyCollection<AdminPlatformUserSearchResponse> _searchedUsers;
        private readonly AdminPlatformDashboardStatsResponse? _stats;

        public FakeAdminPlatformApiClient(
            IReadOnlyCollection<AdminPlatformCafeResponse>? cafes = null,
            IReadOnlyCollection<AdminPlatformCafeMemberResponse>? members = null,
            IReadOnlyCollection<AdminPlatformUserSearchResponse>? searchedUsers = null,
            AdminPlatformDashboardStatsResponse? stats = null)
        {
            _cafes = cafes ?? [];
            _members = members ?? [];
            _searchedUsers = searchedUsers ?? [];
            _stats = stats;
        }

        public int GetCafesCallCount { get; private set; }

        public long? LastGetMembersCafeId { get; private set; }

        public AdminPlatformCreateCafeRequest? LastCreateCafeRequest { get; private set; }

        public long? LastActivatedCafeId { get; private set; }

        public long? LastDeactivatedCafeId { get; private set; }

        public AdminPlatformCreateUserSetupRequest? LastCreateUserSetupRequest { get; private set; }

        public AdminPlatformUserSearchRequest? LastSearchUsersRequest { get; private set; }

        public int SearchUsersCallCount { get; private set; }

        public long? LastReissueUserId { get; private set; }

        public AdminPlatformAssignCafeMemberRequest? LastAssignOwnerRequest { get; private set; }

        public AdminPlatformAssignCafeMemberRequest? LastAssignManagerRequest { get; private set; }

        public long? LastDeactivatedMembershipId { get; private set; }

        public Task<AdminPlatformCafeListResult> GetCafesAsync(CancellationToken cancellationToken)
        {
            GetCafesCallCount++;
            return Task.FromResult(AdminPlatformCafeListResult.Success(_cafes));
        }

        public Task<AdminPlatformDashboardStatsResult> GetPlatformDashboardStatsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_stats is null
                ? AdminPlatformDashboardStatsResult.Failure()
                : AdminPlatformDashboardStatsResult.Success(_stats));
        }

        public Task<AdminPlatformCafeMutationResult> CreateCafeAsync(
            AdminPlatformCreateCafeRequest request,
            CancellationToken cancellationToken)
        {
            LastCreateCafeRequest = request;
            return Task.FromResult(AdminPlatformCafeMutationResult.Success(new AdminPlatformCafeResponse
            {
                Id = 77,
                Name = request.Name,
                Slug = request.Slug ?? "new-platform-cafe",
                IsActive = true,
                IsPublished = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }));
        }

        public Task<AdminPlatformCafeMutationResult> ActivateCafeAsync(long cafeId, CancellationToken cancellationToken)
        {
            LastActivatedCafeId = cafeId;
            return Task.FromResult(AdminPlatformCafeMutationResult.Success(CreatePlatformCafe()));
        }

        public Task<AdminPlatformCafeMutationResult> DeactivateCafeAsync(long cafeId, CancellationToken cancellationToken)
        {
            LastDeactivatedCafeId = cafeId;
            return Task.FromResult(AdminPlatformCafeMutationResult.Success(CreatePlatformCafe(isActive: false)));
        }

        public Task<AdminPlatformMemberListResult> GetCafeMembersAsync(long cafeId, CancellationToken cancellationToken)
        {
            LastGetMembersCafeId = cafeId;
            return Task.FromResult(AdminPlatformMemberListResult.Success(_members));
        }

        public Task<AdminPlatformUserSetupResult> CreateUserSetupAsync(
            AdminPlatformCreateUserSetupRequest request,
            CancellationToken cancellationToken)
        {
            LastCreateUserSetupRequest = request;
            return Task.FromResult(AdminPlatformUserSetupResult.Success(new AdminPlatformUserSetupResponse
            {
                UserId = 120,
                Email = request.Email,
                FullName = request.FullName,
                IsActive = false,
                SetupToken = SetupToken,
                SetupTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
            }));
        }

        public Task<AdminPlatformUserSetupResult> ReissueUserSetupAsync(long userId, CancellationToken cancellationToken)
        {
            LastReissueUserId = userId;
            return Task.FromResult(AdminPlatformUserSetupResult.Success(new AdminPlatformUserSetupResponse
            {
                UserId = userId,
                Email = "owner@example.local",
                FullName = "Owner User",
                IsActive = false,
                SetupToken = SetupToken,
                SetupTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
            }));
        }

        public Task<AdminPlatformUserSearchResult> SearchUsersAsync(
            AdminPlatformUserSearchRequest request,
            CancellationToken cancellationToken)
        {
            LastSearchUsersRequest = request;
            SearchUsersCallCount++;
            return Task.FromResult(AdminPlatformUserSearchResult.Success(_searchedUsers));
        }

        public Task<AdminPlatformMembershipMutationResult> AssignCafeOwnerAsync(
            AdminPlatformAssignCafeMemberRequest request,
            CancellationToken cancellationToken)
        {
            LastAssignOwnerRequest = request;
            return Task.FromResult(AdminPlatformMembershipMutationResult.Success(CreateMembership(request, "CAFE_OWNER")));
        }

        public Task<AdminPlatformMembershipMutationResult> AssignCafeManagerAsync(
            AdminPlatformAssignCafeMemberRequest request,
            CancellationToken cancellationToken)
        {
            LastAssignManagerRequest = request;
            return Task.FromResult(AdminPlatformMembershipMutationResult.Success(CreateMembership(request, "CAFE_MANAGER")));
        }

        public Task<AdminPlatformMembershipMutationResult> DeactivateCafeMembershipAsync(
            long membershipId,
            CancellationToken cancellationToken)
        {
            LastDeactivatedMembershipId = membershipId;
            return Task.FromResult(AdminPlatformMembershipMutationResult.Success(new AdminPlatformMembershipResponse
            {
                Id = membershipId,
                CafeId = 50,
                AppUserId = 120,
                UserEmail = "owner@example.local",
                UserFullName = "Owner User",
                RoleCode = "CAFE_OWNER",
                IsActive = false
            }));
        }

        private static AdminPlatformMembershipResponse CreateMembership(
            AdminPlatformAssignCafeMemberRequest request,
            string roleCode)
        {
            return new AdminPlatformMembershipResponse
            {
                Id = 900,
                CafeId = request.CafeId,
                AppUserId = request.AppUserId,
                UserEmail = "owner@example.local",
                UserFullName = "Owner User",
                RoleCode = roleCode,
                IsActive = true
            };
        }
    }

    private sealed class AdminPlatformWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;
        private readonly IAdminPlatformApiClient _adminPlatformApiClient;

        public AdminPlatformWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient,
            IAdminPlatformApiClient adminPlatformApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
            _adminPlatformApiClient = adminPlatformApiClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Enabled"] = "false",
                    ["ReverseProxy:ForwardLimit"] = "1"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthApiClient>();
                services.AddSingleton(_authApiClient);
                services.RemoveAll<IAdminCafeApiClient>();
                services.AddSingleton(_adminCafeApiClient);
                services.RemoveAll<IAdminPlatformApiClient>();
                services.AddSingleton(_adminPlatformApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-platform-test-data-protection"));
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

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
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

            return _responses.Dequeue();
        }
    }
}
