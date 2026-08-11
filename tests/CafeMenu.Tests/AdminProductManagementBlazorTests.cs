extern alias CafeMenuWeb;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeMenuWeb::CafeMenu.Web.AdminAuth;
using CafeMenuWeb::CafeMenu.Web.AdminCafe;
using CafeMenuWeb::CafeMenu.Web.AdminCategory;
using CafeMenuWeb::CafeMenu.Web.AdminProduct;
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

public sealed class AdminProductManagementBlazorTests
{
    private const string AccessToken = "access-token-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public async Task ProductAdminRoute_ShouldRedirectAnonymousUserToLogin()
    {
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([])),
            new FakeAdminProductApiClient(AdminProductListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/admin/cafes/10/products");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/account/login?returnUrl=%2Fadmin%2Fcafes%2F10%2Fproducts",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AccessibleCafe_ShouldOpenProductPageAndLoadCategoriesAndProducts()
    {
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()]));
        var productClient = new FakeAdminProductApiClient(AdminProductListResult.Success([CreateProduct()]));
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient,
            productClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");

        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, categoryClient.GetCategoriesCallCount);
        Assert.Equal(1, productClient.GetProductsCallCount);
        Assert.Equal(10, categoryClient.LastCafeId);
        Assert.Equal(10, productClient.LastCafeId);
        Assert.Contains("Product Management Cafe", html, StringComparison.Ordinal);
        Assert.Contains("Toast", html, StringComparison.Ordinal);
        Assert.Contains("Meals", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InaccessibleCafe_ShouldRejectBeforeProductApiCall()
    {
        var categoryClient = new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()]));
        var productClient = new FakeAdminProductApiClient(AdminProductListResult.Success([CreateProduct()]));
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            categoryClient,
            productClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/99/products");

        using var response = await client.GetAsync("/admin/cafes/99/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Erişim yok", html, StringComparison.Ordinal);
        Assert.Equal(0, categoryClient.GetCategoriesCallCount);
        Assert.Equal(0, productClient.GetProductsCallCount);
    }

    [Fact]
    public async Task ProductList_ShouldRenderSupportedFieldsAndActions()
    {
        var product = CreateProduct(
            id: 21,
            name: "Cheesecake",
            description: "Daily dessert",
            imageUrl: "https://cdn.example.test/product.png",
            price: 150m,
            displayOrder: 3,
            isVisible: true,
            isAvailable: false,
            isPublished: true);
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()])),
            new FakeAdminProductApiClient(AdminProductListResult.Success([product])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");

        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("Cheesecake", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Daily dessert", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("https://cdn.example.test/product.png", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("150,00 ₺", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Sıra 3", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Tükendi", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Düzenle", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Gizle", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Mevcut yap", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Yay", decodedHtml, StringComparison.Ordinal);
        Assert.Contains(">Sil<", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Yukarı", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("Aşağı", decodedHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductPage_ShouldRenderEmptyState()
    {
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()])),
            new FakeAdminProductApiClient(AdminProductListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");

        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Henüz ürün yok", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductPage_ShouldRenderNoCategoryState()
    {
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([])),
            new FakeAdminProductApiClient(AdminProductListResult.Success([])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");

        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Önce kategori oluşturun", html, StringComparison.Ordinal);
        Assert.Contains("/admin/cafes/10/categories", html, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateProductForm", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductPage_ShouldRenderSafeBackendFailureState()
    {
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory()])),
            new FakeAdminProductApiClient(AdminProductListResult.Failure()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");

        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Ürünler yüklenemedi", html, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProductFormPost_ShouldBindSubmittedValues()
    {
        var productClient = new FakeAdminProductApiClient(AdminProductListResult.Success([]));
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory(id: 31)])),
            productClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");
        using var getResponse = await client.GetAsync("/admin/cafes/10/products");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var formFields = ExtractFormFields(getHtml, "CreateProductForm");
        SetFormField(formFields, "Name", "Tatlılar Test");
        SetFormField(formFields, "CategoryId", "31");
        SetFormField(formFields, "Price", "150.50");
        SetFormField(formFields, "DisplayOrder", "2");
        SetFormField(formFields, "Description", "Günlük tatlı çeşitleri");
        SetFormField(formFields, "ImageUrl", string.Empty);
        SetFormField(formFields, "IsVisible", "true");
        SetFormField(formFields, "IsAvailable", "true");

        using var postResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(formFields));
        var postHtml = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.True(productClient.LastCreateRequest is not null, postHtml);
        Assert.Equal(10, productClient.LastCreateRequest.CafeId);
        Assert.Equal(31, productClient.LastCreateRequest.CategoryId);
        Assert.Equal("Tatlılar Test", productClient.LastCreateRequest.Name);
        Assert.Equal("Günlük tatlı çeşitleri", productClient.LastCreateRequest.Description);
        Assert.Equal(150.50m, productClient.LastCreateRequest.Price);
        Assert.Null(productClient.LastCreateRequest.ImageUrl);
        Assert.Equal(2, productClient.LastCreateRequest.DisplayOrder);
        Assert.True(productClient.LastCreateRequest.IsVisible);
        Assert.True(productClient.LastCreateRequest.IsAvailable);
        Assert.Equal(0, productClient.UpdateProductCallCount);
        Assert.DoesNotContain("Ürün adı zorunludur", postHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductPage_ShouldNotExposeCafeIdOrManualCategoryIdInput()
    {
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory(id: 31)])),
            new FakeAdminProductApiClient(AdminProductListResult.Success([CreateProduct()])));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");

        using var response = await client.GetAsync("/admin/cafes/10/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("name=\"CafeId\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"cafeId\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"number\" name=\"_createModel.CategoryId\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<select", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"_createModel.CategoryId\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateProductFormPost_ShouldBindExistingAndSubmittedValues()
    {
        var product = CreateProduct(id: 21, categoryId: 31, name: "Eski Ürün", description: "Eski açıklama", price: 80m, displayOrder: 1);
        var productClient = new FakeAdminProductApiClient(AdminProductListResult.Success([product]));
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory(id: 31), CreateCategory(id: 32, name: "İçecekler")])),
            productClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");
        using var getResponse = await client.GetAsync("/admin/cafes/10/products");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var actionFields = ExtractFormFields(getHtml, "ProductActionForm");
        SetFormField(actionFields, "_productActionModel.Action", "Edit|21");

        using var editResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(actionFields));
        var editHtml = await editResponse.Content.ReadAsStringAsync();
        Assert.Contains("UpdateProductForm", editHtml, StringComparison.Ordinal);

        var updateFields = ExtractFormFields(editHtml, "UpdateProductForm");
        AssertFormField(updateFields, "ProductId", "21");
        AssertFormField(updateFields, "CategoryId", "31");
        AssertFormField(updateFields, "Name", product.Name);
        AssertFormField(updateFields, "Description", product.Description);
        AssertFormField(updateFields, "Price", "80");
        AssertFormField(updateFields, "DisplayOrder", "1");

        SetFormField(updateFields, "ProductId", "21");
        SetFormField(updateFields, "CategoryId", "32");
        SetFormField(updateFields, "Name", "Güncel Ürün");
        SetFormField(updateFields, "Price", "95.75");
        SetFormField(updateFields, "DisplayOrder", "4");
        SetFormField(updateFields, "Description", "Güncel açıklama");
        SetFormField(updateFields, "ImageUrl", "https://cdn.example.test/urun.png");

        using var updateResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(updateFields));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(1, productClient.UpdateProductCallCount);
        Assert.Equal(21, productClient.LastUpdateProductId);
        Assert.NotNull(productClient.LastUpdateRequest);
        Assert.Equal(10, productClient.LastUpdateRequest.CafeId);
        Assert.Equal(32, productClient.LastUpdateRequest.CategoryId);
        Assert.Equal("Güncel Ürün", productClient.LastUpdateRequest.Name);
        Assert.Equal("Güncel açıklama", productClient.LastUpdateRequest.Description);
        Assert.Equal(95.75m, productClient.LastUpdateRequest.Price);
        Assert.Equal("https://cdn.example.test/urun.png", productClient.LastUpdateRequest.ImageUrl);
        Assert.Equal(4, productClient.LastUpdateRequest.DisplayOrder);
        Assert.Equal(0, productClient.CreateProductCallCount);
    }

    [Fact]
    public async Task StaticSsrActionForms_ShouldHandleVisibilityAvailabilityPublicationDeleteAndReorder()
    {
        var products = new[]
        {
            CreateProduct(id: 21, categoryId: 31, name: "Kahve", displayOrder: 0, isVisible: true, isAvailable: true),
            CreateProduct(id: 22, categoryId: 31, name: "Latte", displayOrder: 1, isVisible: true, isAvailable: false),
            CreateProduct(id: 23, categoryId: 32, name: "Tost", displayOrder: 0, isVisible: true, isAvailable: true)
        };
        var productClient = new FakeAdminProductApiClient(AdminProductListResult.Success(products));
        await using var factory = new AdminProductWebApplicationFactory(
            new FakeAdminAuthApiClient(CreateAuthResponse()),
            new FakeAdminCafeApiClient(AdminCafeListResult.Success([CreateCafe(id: 10)])),
            new FakeAdminCategoryApiClient(AdminCategoryListResult.Success([CreateCategory(id: 31), CreateCategory(id: 32, name: "Yiyecekler")])),
            productClient);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginThroughEndpointAsync(client, "/admin/cafes/10/products");
        using var getResponse = await client.GetAsync("/admin/cafes/10/products");
        var getHtml = await getResponse.Content.ReadAsStringAsync();

        var visibilityFields = ExtractFormFields(getHtml, "ProductActionForm");
        SetFormField(visibilityFields, "_productActionModel.Action", "Visibility|21|false");
        using var visibilityResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(visibilityFields));
        Assert.Equal(HttpStatusCode.OK, visibilityResponse.StatusCode);
        Assert.Equal(21, productClient.LastVisibilityProductId);
        Assert.NotNull(productClient.LastVisibilityRequest);
        Assert.False(productClient.LastVisibilityRequest.IsVisible);

        var availabilityFields = ExtractFormFields(getHtml, "ProductActionForm");
        SetFormField(availabilityFields, "_productActionModel.Action", "Availability|22|true");
        using var availabilityResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(availabilityFields));
        Assert.Equal(HttpStatusCode.OK, availabilityResponse.StatusCode);
        Assert.Equal(22, productClient.LastAvailabilityProductId);
        Assert.NotNull(productClient.LastAvailabilityRequest);
        Assert.True(productClient.LastAvailabilityRequest.IsAvailable);

        var publicationFields = ExtractFormFields(getHtml, "ProductActionForm");
        SetFormField(publicationFields, "_productActionModel.Action", "Publication|21|true");
        using var publicationResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(publicationFields));
        Assert.Equal(HttpStatusCode.OK, publicationResponse.StatusCode);
        Assert.Equal(21, productClient.LastPublicationProductId);
        Assert.NotNull(productClient.LastPublicationRequest);
        Assert.Equal(10, productClient.LastPublicationRequest.CafeId);
        Assert.True(productClient.LastPublicationRequest.IsPublished);

        var confirmDeleteFields = ExtractFormFields(getHtml, "ProductActionForm");
        SetFormField(confirmDeleteFields, "_productActionModel.Action", "ConfirmDelete|21");
        using var confirmResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(confirmDeleteFields));
        var confirmHtml = await confirmResponse.Content.ReadAsStringAsync();
        Assert.Equal(0, productClient.DeleteProductCallCount);
        Assert.Contains("soft-delete", confirmHtml, StringComparison.Ordinal);

        var deleteFields = ExtractFormFields(confirmHtml, "ProductActionForm");
        SetFormField(deleteFields, "_productActionModel.Action", "Delete|21");
        using var deleteResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(deleteFields));
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(1, productClient.DeleteProductCallCount);
        Assert.Equal(21, productClient.LastDeleteProductId);

        var moveFields = ExtractFormFields(getHtml, "ProductActionForm");
        SetFormField(moveFields, "_productActionModel.Action", "Move|22|-1");
        using var moveResponse = await client.PostAsync(
            "/admin/cafes/10/products",
            new FormUrlEncodedContent(moveFields));
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        Assert.NotNull(productClient.LastReorderRequest);
        Assert.Equal(10, productClient.LastReorderRequest.CafeId);
        Assert.Equal(31, productClient.LastReorderRequest.CategoryId);
        Assert.Collection(
            productClient.LastReorderRequest.Products,
            order =>
            {
                Assert.Equal(22, order.ProductId);
                Assert.Equal(0, order.DisplayOrder);
            },
            order =>
            {
                Assert.Equal(21, order.ProductId);
                Assert.Equal(1, order.DisplayOrder);
            });
        Assert.DoesNotContain(productClient.LastReorderRequest.Products, order => order.ProductId == 23);
    }

    [Fact]
    public void ProductPage_ShouldUseStaticSsrCompatibleForms()
    {
        var pageSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminProductManagementPage.razor"));

        Assert.Contains("[SupplyParameterFromForm(FormName = \"CreateProductForm\", Name = \"_createModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromForm(FormName = \"UpdateProductForm\", Name = \"_editModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("[SupplyParameterFromForm(FormName = \"ProductActionForm\", Name = \"_productActionModel\")]", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"CreateProductForm\"", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"UpdateProductForm\"", pageSource, StringComparison.Ordinal);
        Assert.Contains("FormName=\"ProductActionForm\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"button\"", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", pageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MarkupString", pageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CafeShell_ShouldLinkToProductManagement()
    {
        var shellSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CafeMenu.Web",
            "Components",
            "Pages",
            "AdminCafeShellPage.razor"));

        Assert.Contains("/products", shellSource, StringComparison.Ordinal);
        Assert.Contains("Ürünler", shellSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminProductApiClient_ShouldUseAuthenticatedAdminHttpClientAndBackendRoutes()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("""
            {
              "success": true,
              "message": "ok",
              "data": [
                {
                  "id": 2,
                  "cafeId": 10,
                  "categoryId": 20,
                  "name": "Tea",
                  "description": null,
                  "price": 30,
                  "imageUrl": null,
                  "isAvailable": true,
                  "isVisible": true,
                  "isPublished": false,
                  "displayOrder": 1,
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
                "categoryId": 20,
                "name": "Tea",
                "description": null,
                "price": 30,
                "imageUrl": null,
                "isAvailable": true,
                "isVisible": true,
                "isPublished": false,
                "displayOrder": 1,
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
                "categoryId": 20,
                "name": "Tea Updated",
                "description": null,
                "price": 35,
                "imageUrl": null,
                "isAvailable": false,
                "isVisible": false,
                "isPublished": true,
                "displayOrder": 2,
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
                "categoryId": 20,
                "name": "Tea Updated",
                "description": null,
                "price": 35,
                "imageUrl": null,
                "isAvailable": true,
                "isVisible": true,
                "isPublished": false,
                "displayOrder": 2,
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
                "categoryId": 20,
                "name": "Tea Updated",
                "description": null,
                "price": 35,
                "imageUrl": null,
                "isAvailable": true,
                "isVisible": false,
                "isPublished": false,
                "displayOrder": 2,
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
                "categoryId": 20,
                "name": "Tea Updated",
                "description": null,
                "price": 35,
                "imageUrl": null,
                "isAvailable": false,
                "isVisible": false,
                "isPublished": false,
                "displayOrder": 2,
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
                  "categoryId": 20,
                  "name": "Tea Updated",
                  "description": null,
                  "price": 35,
                  "imageUrl": null,
                  "isAvailable": false,
                  "isVisible": false,
                  "isPublished": false,
                  "displayOrder": 0,
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
        var apiClient = new AdminProductApiClient(httpClientFactory);

        await apiClient.GetProductsAsync(10, CancellationToken.None);
        await apiClient.CreateProductAsync(
            new AdminCreateProductRequest(10, 20, "Tea", null, 30m, null, true, true, 1),
            CancellationToken.None);
        await apiClient.UpdateProductAsync(
            2,
            new AdminUpdateProductRequest(10, 20, "Tea Updated", null, 35m, null, 2),
            CancellationToken.None);
        await apiClient.ChangeProductVisibilityAsync(
            2,
            new AdminChangeProductVisibilityRequest(10, false),
            CancellationToken.None);
        await apiClient.ChangeProductAvailabilityAsync(
            2,
            new AdminChangeProductAvailabilityRequest(10, false),
            CancellationToken.None);
        await apiClient.ChangeProductPublicationAsync(
            2,
            new AdminChangeProductPublicationRequest(10, true),
            CancellationToken.None);
        await apiClient.ReorderProductsAsync(
            new AdminReorderProductsRequest(10, 20, [new AdminProductOrderRequest(2, 0)]),
            CancellationToken.None);
        await apiClient.DeleteProductAsync(2, CancellationToken.None);

        Assert.Equal(AdminAuthenticationConstants.AdminApiClientName, httpClientFactory.LastClientName);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://api.example.test/Product/GetProducts/10", request.Uri);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.example.test/Product/CreateProduct", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"categoryId\":20");
                AssertJsonContains(request.Body, "\"name\":\"Tea\"");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Product/UpdateProduct/2", request.Uri);
                AssertJsonContains(request.Body, "\"price\":35");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Product/ChangeProductVisibility/2", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"isVisible\":false");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Product/ChangeProductAvailability/2", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"isAvailable\":false");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Product/ChangeProductPublication/2", request.Uri);
                AssertJsonContains(request.Body, "\"cafeId\":10");
                AssertJsonContains(request.Body, "\"isPublished\":true");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://api.example.test/Product/ReorderProducts", request.Uri);
                AssertJsonContains(request.Body, "\"categoryId\":20");
                AssertJsonContains(request.Body, "\"productId\":2");
            },
            request =>
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Equal("https://api.example.test/Product/DeleteProduct/2", request.Uri);
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
            Name = "Product Management Cafe",
            Slug = "product-management-cafe",
            IsActive = true,
            IsPublished = false,
            RoleCodes = [ "CAFE_OWNER" ]
        };
    }

    private static AdminCategoryResponse CreateCategory(
        long id = 31,
        long cafeId = 10,
        string name = "Meals",
        int displayOrder = 0)
    {
        return new AdminCategoryResponse
        {
            Id = id,
            CafeId = cafeId,
            Name = name,
            Description = null,
            ImageUrl = null,
            DisplayOrder = displayOrder,
            IsVisible = true,
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static AdminProductResponse CreateProduct(
        long id = 21,
        long cafeId = 10,
        long categoryId = 31,
        string name = "Toast",
        string? description = "Cheese toast",
        string? imageUrl = null,
        decimal price = 85.50m,
        int displayOrder = 0,
        bool isVisible = true,
        bool isAvailable = true,
        bool isPublished = false)
    {
        return new AdminProductResponse
        {
            Id = id,
            CafeId = cafeId,
            CategoryId = categoryId,
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            Price = price,
            DisplayOrder = displayOrder,
            IsVisible = isVisible,
            IsAvailable = isAvailable,
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
                "AdminProductManagementPage.razor");

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

        public long? LastCafeId { get; private set; }

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
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryMutationResult> UpdateCategoryAsync(
            long categoryId,
            AdminUpdateCategoryRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryDeleteResult> DeleteCategoryAsync(long categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryDeleteResult.Failure());
        }

        public Task<AdminCategoryMutationResult> ChangeCategoryVisibilityAsync(
            long categoryId,
            AdminChangeCategoryVisibilityRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryMutationResult> ChangeCategoryPublicationAsync(
            long categoryId,
            AdminChangeCategoryPublicationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryMutationResult.Failure());
        }

        public Task<AdminCategoryListResult> ReorderCategoriesAsync(
            AdminReorderCategoriesRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminCategoryListResult.Failure());
        }
    }

    private sealed class FakeAdminProductApiClient : IAdminProductApiClient
    {
        private readonly AdminProductListResult _listResult;

        public FakeAdminProductApiClient(AdminProductListResult listResult)
        {
            _listResult = listResult;
        }

        public int GetProductsCallCount { get; private set; }

        public int CreateProductCallCount { get; private set; }

        public int UpdateProductCallCount { get; private set; }

        public int DeleteProductCallCount { get; private set; }

        public long? LastCafeId { get; private set; }

        public long? LastUpdateProductId { get; private set; }

        public long? LastVisibilityProductId { get; private set; }

        public long? LastAvailabilityProductId { get; private set; }

        public long? LastPublicationProductId { get; private set; }

        public long? LastDeleteProductId { get; private set; }

        public AdminCreateProductRequest? LastCreateRequest { get; private set; }

        public AdminUpdateProductRequest? LastUpdateRequest { get; private set; }

        public AdminChangeProductVisibilityRequest? LastVisibilityRequest { get; private set; }

        public AdminChangeProductAvailabilityRequest? LastAvailabilityRequest { get; private set; }

        public AdminChangeProductPublicationRequest? LastPublicationRequest { get; private set; }

        public AdminReorderProductsRequest? LastReorderRequest { get; private set; }

        public Task<AdminProductListResult> GetProductsAsync(long cafeId, CancellationToken cancellationToken)
        {
            GetProductsCallCount++;
            LastCafeId = cafeId;
            return Task.FromResult(_listResult);
        }

        public Task<AdminProductMutationResult> CreateProductAsync(
            AdminCreateProductRequest request,
            CancellationToken cancellationToken)
        {
            CreateProductCallCount++;
            LastCreateRequest = request;
            return Task.FromResult(AdminProductMutationResult.Success(CreateProduct(name: request.Name, categoryId: request.CategoryId, price: request.Price)));
        }

        public Task<AdminProductMutationResult> UpdateProductAsync(
            long productId,
            AdminUpdateProductRequest request,
            CancellationToken cancellationToken)
        {
            UpdateProductCallCount++;
            LastUpdateProductId = productId;
            LastUpdateRequest = request;
            return Task.FromResult(AdminProductMutationResult.Success(CreateProduct(id: productId, name: request.Name, categoryId: request.CategoryId, price: request.Price)));
        }

        public Task<AdminProductDeleteResult> DeleteProductAsync(long productId, CancellationToken cancellationToken)
        {
            DeleteProductCallCount++;
            LastDeleteProductId = productId;
            return Task.FromResult(AdminProductDeleteResult.Success());
        }

        public Task<AdminProductMutationResult> ChangeProductVisibilityAsync(
            long productId,
            AdminChangeProductVisibilityRequest request,
            CancellationToken cancellationToken)
        {
            LastVisibilityProductId = productId;
            LastVisibilityRequest = request;
            return Task.FromResult(AdminProductMutationResult.Success(CreateProduct(id: productId, isVisible: request.IsVisible)));
        }

        public Task<AdminProductMutationResult> ChangeProductAvailabilityAsync(
            long productId,
            AdminChangeProductAvailabilityRequest request,
            CancellationToken cancellationToken)
        {
            LastAvailabilityProductId = productId;
            LastAvailabilityRequest = request;
            return Task.FromResult(AdminProductMutationResult.Success(CreateProduct(id: productId, isAvailable: request.IsAvailable)));
        }

        public Task<AdminProductMutationResult> ChangeProductPublicationAsync(
            long productId,
            AdminChangeProductPublicationRequest request,
            CancellationToken cancellationToken)
        {
            LastPublicationProductId = productId;
            LastPublicationRequest = request;
            return Task.FromResult(AdminProductMutationResult.Success(CreateProduct(id: productId, isPublished: request.IsPublished)));
        }

        public Task<AdminProductListResult> ReorderProductsAsync(
            AdminReorderProductsRequest request,
            CancellationToken cancellationToken)
        {
            LastReorderRequest = request;
            var products = request.Products
                .Select(product => CreateProduct(id: product.ProductId, categoryId: request.CategoryId, displayOrder: product.DisplayOrder))
                .ToArray();

            return Task.FromResult(AdminProductListResult.Success(products));
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

    private sealed class AdminProductWebApplicationFactory : WebApplicationFactory<WebProgram>
    {
        private readonly IAdminAuthApiClient _authApiClient;
        private readonly IAdminCafeApiClient _adminCafeApiClient;
        private readonly IAdminCategoryApiClient _adminCategoryApiClient;
        private readonly IAdminProductApiClient _adminProductApiClient;

        public AdminProductWebApplicationFactory(
            IAdminAuthApiClient authApiClient,
            IAdminCafeApiClient adminCafeApiClient,
            IAdminCategoryApiClient adminCategoryApiClient,
            IAdminProductApiClient adminProductApiClient)
        {
            _authApiClient = authApiClient;
            _adminCafeApiClient = adminCafeApiClient;
            _adminCategoryApiClient = adminCategoryApiClient;
            _adminProductApiClient = adminProductApiClient;
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
                services.RemoveAll<IAdminProductApiClient>();
                services.AddSingleton(_adminProductApiClient);
                services.RemoveAll<IPublicMenuApiClient>();
                services.AddSingleton<IPublicMenuApiClient>(new StubPublicMenuApiClient());

                var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-admin-product-test-data-protection"));
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
