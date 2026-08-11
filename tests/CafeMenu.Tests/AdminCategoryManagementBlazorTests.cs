extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminCategory;
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

public sealed class AdminCategoryManagementBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public async Task CategoryAdminRoute_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin/cafes/10/categories");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/account/login?returnUrl=%2Fadmin%2Fcafes%2F10%2Fcategories",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AccessibleCafe_ShouldOpenCategoryPageAndLoadCategories()
    {
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()]));
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");

        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, categoryClient.GetCategoriesCallCount);
        Assert.Equal(10, categoryClient.LastCafeId);
        Assert.Contains("Category Management Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Breakfast", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InaccessibleCafe_ShouldRejectBeforeCategoryApiCall()
    {
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()]));
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/categories");

        using var response = await client.GetAsync("/admin/cafes/99/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.Equal(0, categoryClient.GetCategoriesCallCount);
    }

    [Fact]
    public async Task CategoryList_ShouldRenderSupportedFieldsAndActions()
    {
        var category = CreateCategory(
            id: 21,
            name: "Breakfast",
            description: "Morning plates",
            imageUrl: "https://cdn.example.test/category.png",
            displayOrder: 3,
            isVisible: true,
            isPublished: true);
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([category])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");

        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Breakfast", html, StringComparison.Ordinal);
        Assert.Contains("Morning plates", html, StringComparison.Ordinal);
        Assert.Contains("https://cdn.example.test/category.png", html, StringComparison.Ordinal);
        Assert.Contains("Sıra 3", html, StringComparison.Ordinal);
        Assert.Contains("Düzenle", html, StringComparison.Ordinal);
        Assert.Contains("Gizle", html, StringComparison.Ordinal);
        Assert.Contains("Yay", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains(">Sil<", html, StringComparison.Ordinal);
        Assert.Contains("Yukarı", html, StringComparison.Ordinal);
        Assert.Contains("Aşağı", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CategoryPage_ShouldRenderEmptyState()
    {
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");

        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Henüz kategori yok", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CategoryPage_ShouldRenderSafeBackendFailureState()
    {
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");

        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Kategoriler yüklenemedi", html, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CategoryPage_ShouldNotExposeCafeIdAsUserEditableFormInput()
    {
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");

        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("name=\"CafeId\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"cafeId\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCategoryFormPost_ShouldBindSubmittedValues()
    {
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([]));
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");
        using var getResponse = await client.GetAsync("/admin/cafes/10/categories");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var formFields = ExtractFormFields(getHtml, "CreateCategoryForm");
        SetFormField(formFields, "Name", "Tatlılar Test");
        SetFormField(formFields, "DisplayOrder", "2");
        SetFormField(formFields, "Description", "Günlük tatlı çeşitleri");
        SetFormField(formFields, "ImageUrl", string.Empty);
        SetFormField(formFields, "IsVisible", "true");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(formFields));
        var postHtml = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.True(
            categoryClient.LastCreateRequest is not null,
            string.Join(", ", formFields.Select(field => $"{field.Key}={field.Value}")) + "\n" + postHtml);
        Assert.Equal(10, categoryClient.LastCreateRequest.CafeId);
        Assert.Equal("Tatlılar Test", categoryClient.LastCreateRequest.Name);
        Assert.Equal("Günlük tatlı çeşitleri", categoryClient.LastCreateRequest.Description);
        Assert.Null(categoryClient.LastCreateRequest.ImageUrl);
        Assert.Equal(2, categoryClient.LastCreateRequest.DisplayOrder);
        Assert.True(categoryClient.LastCreateRequest.IsVisible);
        Assert.Equal(0, categoryClient.UpdateCategoryCallCount);
        Assert.DoesNotContain("Kategori adı zorunludur", postHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCategoryFormPost_ShouldBindExistingAndSubmittedValues()
    {
        var category = CreateCategory(id: 21, name: "Tatlılar", description: "Eski açıklama", displayOrder: 1);
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([category]));
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");
        using var getResponse = await client.GetAsync("/admin/cafes/10/categories");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var actionFields = ExtractFormFields(getHtml, "CategoryActionForm");
        SetFormField(actionFields, "_categoryActionModel.Action", "Edit|21");

        using var editResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(actionFields));
        var editHtml = await editResponse.Content.ReadAsStringAsync();
        Assert.Contains("UpdateCategoryForm", editHtml, StringComparison.Ordinal);

        var updateFields = ExtractFormFields(editHtml, "UpdateCategoryForm");
        AssertFormField(updateFields, "CategoryId", "21");
        AssertFormField(updateFields, "Name", category.Name);
        AssertFormField(updateFields, "Description", category.Description);
        AssertFormField(updateFields, "DisplayOrder", category.DisplayOrder.ToString());

        SetFormField(updateFields, "CategoryId", "21");
        SetFormField(updateFields, "Name", "Tatlılar Güncel");
        SetFormField(updateFields, "DisplayOrder", "4");
        SetFormField(updateFields, "Description", "Güncel açıklama");
        SetFormField(updateFields, "ImageUrl", "https://cdn.example.test/tatlilar.png");

        using var updateResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(updateFields));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(1, categoryClient.UpdateCategoryCallCount);
        Assert.Equal(21, categoryClient.LastUpdateCategoryId);
        Assert.NotNull(categoryClient.LastUpdateRequest);
        Assert.Equal(10, categoryClient.LastUpdateRequest.CafeId);
        Assert.Equal("Tatlılar Güncel", categoryClient.LastUpdateRequest.Name);
        Assert.Equal("Güncel açıklama", categoryClient.LastUpdateRequest.Description);
        Assert.Equal("https://cdn.example.test/tatlilar.png", categoryClient.LastUpdateRequest.ImageUrl);
        Assert.Equal(4, categoryClient.LastUpdateRequest.DisplayOrder);
        Assert.Equal(0, categoryClient.CreateCategoryCallCount);
    }

    [Fact]
    public async Task StaticSsrActionForms_ShouldHandleVisibilityPublicationDeleteAndReorder()
    {
        var categories = new[]
        {
            CreateCategory(id: 21, name: "Kahveler", displayOrder: 0, isVisible: true),
            CreateCategory(id: 22, name: "Tatlılar", displayOrder: 1, isVisible: true)
        };
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success(categories));
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");
        using var getResponse = await client.GetAsync("/admin/cafes/10/categories");
        var getHtml = await getResponse.Content.ReadAsStringAsync();

        var visibilityFields = ExtractFormFields(getHtml, "CategoryActionForm");
        SetFormField(visibilityFields, "_categoryActionModel.Action", "Visibility|21|false");
        using var visibilityResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(visibilityFields));
        Assert.Equal(HttpStatusCode.OK, visibilityResponse.StatusCode);
        Assert.Equal(21, categoryClient.LastVisibilityCategoryId);
        Assert.NotNull(categoryClient.LastVisibilityRequest);
        Assert.False(categoryClient.LastVisibilityRequest.IsVisible);

        var publicationFields = ExtractFormFields(getHtml, "CategoryActionForm");
        SetFormField(publicationFields, "_categoryActionModel.Action", "Publication|21|true");
        using var publicationResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(publicationFields));
        Assert.Equal(HttpStatusCode.OK, publicationResponse.StatusCode);
        Assert.Equal(21, categoryClient.LastPublicationCategoryId);
        Assert.NotNull(categoryClient.LastPublicationRequest);
        Assert.Equal(10, categoryClient.LastPublicationRequest.CafeId);
        Assert.True(categoryClient.LastPublicationRequest.IsPublished);

        var confirmDeleteFields = ExtractFormFields(getHtml, "CategoryActionForm");
        SetFormField(confirmDeleteFields, "_categoryActionModel.Action", "ConfirmDelete|21");
        using var confirmResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(confirmDeleteFields));
        var confirmHtml = await confirmResponse.Content.ReadAsStringAsync();
        Assert.Equal(0, categoryClient.DeleteCategoryCallCount);
        Assert.Contains("soft-delete", confirmHtml, StringComparison.Ordinal);

        var deleteFields = ExtractFormFields(confirmHtml, "CategoryActionForm");
        SetFormField(deleteFields, "_categoryActionModel.Action", "Delete|21");
        using var deleteResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(deleteFields));
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(1, categoryClient.DeleteCategoryCallCount);
        Assert.Equal(21, categoryClient.LastDeleteCategoryId);

        var moveFields = ExtractFormFields(getHtml, "CategoryActionForm");
        SetFormField(moveFields, "_categoryActionModel.Action", "Move|22|-1");
        using var moveResponse = await client.PostAsync(
            "/admin/cafes/10/categories",
            new FormUrlEncodedContent(moveFields));
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        Assert.NotNull(categoryClient.LastReorderRequest);
        Assert.Equal(10, categoryClient.LastReorderRequest.CafeId);
        Assert.Collection(
            categoryClient.LastReorderRequest.Categories,
            order =>
            {
                Assert.Equal(22, order.CategoryId);
                Assert.Equal(0, order.DisplayOrder);
            },
            order =>
            {
                Assert.Equal(21, order.CategoryId);
                Assert.Equal(1, order.DisplayOrder);
            });
    }

    [Fact]
    public void CategoryPage_ShouldUseStaticSsrCompatibleForms()
    {
        var pageSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminCategoryManagementPage.razor"));

        Assert.Contains("[SupplyParameterFromForm(FormName = \"CreateCategoryForm\", Name = \"_createModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromForm(FormName = \"UpdateCategoryForm\", Name = \"_editModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromForm(FormName = \"CategoryActionForm\", Name = \"_categoryActionModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"CreateCategoryForm\"", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"UpdateCategoryForm\"", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"CategoryActionForm\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", pageSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CategoryPage_ShouldNotUseBrowserStorageOrRawHtml()
    {
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/categories");

        using var response = await client.GetAsync("/admin/cafes/10/categories");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("localStorage", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CafeShell_ShouldLinkToCategoryManagement()
    {
        await using var factory = new AdminCategoryWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10");

        using var response = await client.GetAsync("/admin/cafes/10");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("/admin/cafes/10/categories", html, StringComparison.Ordinal);
        Assert.Contains("Kategoriler", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCategoryApiClient_ShouldCallCurrentBackendCategoryEndpoints()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": [
                {
                  "id": 1,
                  "cafeId": 10,
                  "name": "Breakfast",
                  "description": "Morning",
                  "imageUrl": null,
                  "displayOrder": 0,
                  "isVisible": true,
                  "isPublished": false,
                  "createdAt": "2026-08-10T00:00:00+00:00",
                  "updatedAt": "2026-08-10T00:00:00+00:00"
                }
              ]
            }
            """),
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": {
                "id": 2,
                "cafeId": 10,
                "name": "Lunch",
                "description": null,
                "imageUrl": null,
                "displayOrder": 1,
                "isVisible": true,
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
                "id": 2,
                "cafeId": 10,
                "name": "Lunch Updated",
                "description": null,
                "imageUrl": null,
                "displayOrder": 2,
                "isVisible": false,
                "isPublished": true,
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
                "id": 2,
                "cafeId": 10,
                "name": "Lunch Updated",
                "description": null,
                "imageUrl": null,
                "displayOrder": 2,
                "isVisible": true,
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
                "id": 2,
                "cafeId": 10,
                "name": "Lunch Updated",
                "description": null,
                "imageUrl": null,
                "displayOrder": 2,
                "isVisible": false,
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
              "data": [
                {
                  "id": 2,
                  "cafeId": 10,
                  "name": "Lunch Updated",
                  "description": null,
                  "imageUrl": null,
                  "displayOrder": 0,
                  "isVisible": false,
                  "isPublished": false,
                  "createdAt": "2026-08-10T00:00:00+00:00",
                  "updatedAt": "2026-08-10T00:00:00+00:00"
                }
              ]
            }
            """),
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": null
            }
            """));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var httpClientFactory = new RecordingHttpClientFactory(httpClient);
        var apiClient = new AdminCategoryApiClient(httpClientFactory);

        await apiClient.GetCategoriesAsync(10, CancellationToken.None);
        await apiClient.CreateCategoryAsync(
            new AdminCreateCategoryRequest(10, "Lunch", null, null, 1, true),
            CancellationToken.None);
        await apiClient.UpdateCategoryAsync(
            2,
            new AdminUpdateCategoryRequest(10, "Lunch Updated", null, null, 2),
            CancellationToken.None);
        await apiClient.ChangeCategoryVisibilityAsync(
            2,
            new AdminChangeCategoryVisibilityRequest(10, false),
            CancellationToken.None);
        await apiClient.ChangeCategoryPublicationAsync(
            2,
            new AdminChangeCategoryPublicationRequest(10, true),
            CancellationToken.None);
        await apiClient.ReorderCategoriesAsync(
            new AdminReorderCategoriesRequest(10, [new AdminCategoryOrderRequest(2, 0)]),
            CancellationToken.None);
        await apiClient.DeleteCategoryAsync(2, CancellationToken.None);

        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, httpClientFactory.LastClientName);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://api.example.test/Category/GetCategories/10", request.Uri);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.example.test/Category/CreateCategory", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"name\":\"Lunch\"");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Category/UpdateCategory/2", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"displayOrder\":2");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Category/ChangeCategoryVisibility/2", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"isVisible\":false");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Category/ChangeCategoryPublication/2", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"isPublished\":true");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Category/ReorderCategories", request.Uri);
                AssertJsonContains(request.Body, "\"categoryId\":2");
                AssertJsonContains(request.Body, "\"displayOrder\":0");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Equal("https://api.example.test/Category/DeleteCategory/2", request.Uri);
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
            var inputs = Regex.Matches(
                form.Groups["body"].Value,
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
                form.Groups["body"].Value,
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
            Name = "Category Management Cafe",
            Slug = "category-management-cafe",
            IsActive = true,
            IsPublished = false,
            RoleCodes = [ "CAFE_OWNER" ]
        };
    }

    private static AdminCategoryResponse CreateCategory(
        long id = 21,
        long cafeId = 10,
        string name = "Breakfast",
        string? description = "Morning plates",
        string? imageUrl = null,
        int displayOrder = 0,
        bool isVisible = true,
        bool isPublished = false)
    {
        return new AdminCategoryResponse
        {
            Id = id,
            CafeId = cafeId,
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder,
            IsVisible = isVisible,
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
                "AdminCategoryManagementPage.razor");

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

    private sealed class FakeAdminCategoryApiClient : IAdminCategoryApiClient
    {
        private readonly AdminCategoryListResult _listResult;

        public FakeAdminCategoryApiClient(AdminCategoryListResult listResult)
        {
            _listResult = listResult;
        }

        public int GetCategoriesCallCount { get; private set; }

        public int CreateCategoryCallCount { get; private set; }

        public int UpdateCategoryCallCount { get; private set; }

        public int DeleteCategoryCallCount { get; private set; }

        public long? LastCafeId { get; private set; }

        public long? LastUpdateCategoryId { get; private set; }

        public long? LastVisibilityCategoryId { get; private set; }

        public long? LastPublicationCategoryId { get; private set; }

        public long? LastDeleteCategoryId { get; private set; }

        public AdminCreateCategoryRequest? LastCreateRequest { get; private set; }

        public AdminUpdateCategoryRequest? LastUpdateRequest { get; private set; }

        public AdminChangeCategoryVisibilityRequest? LastVisibilityRequest { get; private set; }

        public AdminChangeCategoryPublicationRequest? LastPublicationRequest { get; private set; }

        public AdminReorderCategoriesRequest? LastReorderRequest { get; private set; }

        public Task<AdminCategoryListResult> GetCategoriesAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetCategoriesCallCount++;
            LastCafeId = cafeId;
            return Task.FromResult(_listResult);
        }

        public Task<AdminCategoryMutationResult> CreateCategoryAsync(
            AdminCreateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            CreateCategoryCallCount++;
            LastCreateRequest = request;
            return Task.FromResult(AdminCategoryMutationResult.Success(CreateCategory(name: request.Name)));
        }

        public Task<AdminCategoryMutationResult> UpdateCategoryAsync(
            long categoryId,
            AdminUpdateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            UpdateCategoryCallCount++;
            LastUpdateCategoryId = categoryId;
            LastUpdateRequest = request;
            return Task.FromResult(AdminCategoryMutationResult.Success(CreateCategory(id: categoryId, name: request.Name)));
        }

        public Task<AdminCategoryDeleteResult> DeleteCategoryAsync(long categoryId, CancellationToken cancellationToken)
        {
            DeleteCategoryCallCount++;
            LastDeleteCategoryId = categoryId;
            return Task.FromResult(AdminCategoryDeleteResult.Success());
        }

        public Task<AdminCategoryMutationResult> ChangeCategoryVisibilityAsync(
            long categoryId,
            AdminChangeCategoryVisibilityRequest request,
            CancellationToken cancellationToken)
        {
            LastVisibilityCategoryId = categoryId;
            LastVisibilityRequest = request;
            return Task.FromResult(AdminCategoryMutationResult.Success(CreateCategory(id: categoryId, isVisible: request.IsVisible)));
        }

        public Task<AdminCategoryMutationResult> ChangeCategoryPublicationAsync(
            long categoryId,
            AdminChangeCategoryPublicationRequest request,
            CancellationToken cancellationToken)
        {
            LastPublicationCategoryId = categoryId;
            LastPublicationRequest = request;
            return Task.FromResult(AdminCategoryMutationResult.Success(CreateCategory(id: categoryId, isPublished: request.IsPublished)));
        }

        public Task<AdminCategoryListResult> ReorderCategoriesAsync(
            AdminReorderCategoriesRequest request,
            CancellationToken cancellationToken)
        {
            LastReorderRequest = request;
            var categories = request.Categories
                .Select(category => CreateCategory(id: category.CategoryId, displayOrder: category.DisplayOrder))
                .ToArray();

            return Task.FromResult(AdminCategoryListResult.Success(categories));
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

    private sealed class AdminCategoryWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;
        private readonly IAdminCategoryApiClient _adminCategoryApiClient;

        public AdminCategoryWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient,
            IAdminCategoryApiClient adminCategoryApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
            _adminCategoryApiClient = adminCategoryApiClient;
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
                services.RemoveAll<IAdminCategoryApiClient>();
                services.AddSingleton(_adminCategoryApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-category-test-data-protection"));
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
