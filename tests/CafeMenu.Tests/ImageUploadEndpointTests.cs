using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CafeMenu.Api.Data;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Services;
using CafeMenu.Api.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace CafeMenu.Tests;

public sealed class ImageUploadEndpointTests
{
    private const string ValidPassword = "SecurePassword123!";

    [Fact]
    public async Task CategoryImageUpload_ShouldStoreManagedImageAndExposePublicMediaUrl()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "image-category-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Image Category Cafe", "image-category-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Desserts", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsync(
            $"/Category/UploadCategoryImage/{category.Id}",
            CreateMultipartImageContent("../../../client-name.png", "image/png", CreateImageBytes(SKEncodedImageFormat.Png)));
        var json = await ParseAsync(response);
        var imageUrl = json.RootElement.GetProperty("data").GetProperty("imageUrl").GetString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(imageUrl);
        Assert.StartsWith("http://localhost/media/categories/", imageUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("client-name", imageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"http://localhost/media/categories/[a-f0-9]{32}\.png", imageUrl);

        using var mediaResponse = await client.GetAsync(new Uri(imageUrl).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, mediaResponse.StatusCode);
        Assert.Equal("image/png", mediaResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ProductImageUpload_ShouldRejectCrossTenantAccess()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "image-product-cross@example.com");
        var cafeA = await SeedCafeAsync(factory, "Image Product Cafe A", "image-product-cafe-a");
        var cafeB = await SeedCafeAsync(factory, "Image Product Cafe B", "image-product-cafe-b");
        var categoryB = await SeedCategoryAsync(factory, cafeB.Id, "Private Products", 1);
        var productB = await SeedProductAsync(factory, cafeB.Id, categoryB.Id, "Private Product", 1, 25m);
        await SeedMembershipAsync(factory, owner.Id, cafeA.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsync(
            $"/Product/UploadProductImage/{productB.Id}",
            CreateMultipartImageContent("product.png", "image/png", CreateImageBytes(SKEncodedImageFormat.Png)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CafeLogoUpload_ShouldAcceptJpegPngAndWebpFormats()
    {
        await using var factory = new CustomWebApplicationFactory();
        var manager = await SeedUserAsync(factory, "image-branding-manager@example.com");
        var cafe = await SeedCafeAsync(factory, "Image Branding Cafe", "image-branding-cafe");
        await SeedMembershipAsync(factory, manager.Id, cafe.Id, ApplicationRoles.CafeManager);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, manager.Email);

        var cases = new[]
        {
            ("logo.jpg", "image/jpeg", CreateImageBytes(SKEncodedImageFormat.Jpeg), ".jpg"),
            ("logo.png", "image/png", CreateImageBytes(SKEncodedImageFormat.Png), ".png"),
            ("logo.webp", "image/webp", CreateImageBytes(SKEncodedImageFormat.Webp), ".webp")
        };

        foreach (var (fileName, contentType, bytes, expectedExtension) in cases)
        {
            using var response = await client.PostAsync(
                $"/CafeBranding/UploadLogoImage/{cafe.Id}",
                CreateMultipartImageContent(fileName, contentType, bytes));
            var json = await ParseAsync(response);
            var imageUrl = json.RootElement.GetProperty("data").GetProperty("logoImageUrl").GetString();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(imageUrl);
            Assert.EndsWith(expectedExtension, imageUrl, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PlatformAdmin_ShouldUploadCafeCoverWithoutMembership()
    {
        await using var factory = new CustomWebApplicationFactory();
        var platformAdmin = await SeedUserAsync(
            factory,
            "image-platform-admin@example.com",
            platformRoleCode: ApplicationRoles.PlatformAdmin);
        var cafe = await SeedCafeAsync(factory, "Image Platform Cafe", "image-platform-cafe");
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, platformAdmin.Email);

        using var response = await client.PostAsync(
            $"/CafeBranding/UploadCoverImage/{cafe.Id}",
            CreateMultipartImageContent("cover.png", "image/png", CreateImageBytes(SKEncodedImageFormat.Png)));
        var json = await ParseAsync(response);
        var coverImageUrl = json.RootElement.GetProperty("data").GetProperty("coverImageUrl").GetString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("http://localhost/media/cafe-covers/", coverImageUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadImage_ShouldRejectUnsupportedExtensionAndInvalidSignature()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "image-invalid-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Image Invalid Cafe", "image-invalid-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Invalid Image", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var svgResponse = await client.PostAsync(
            $"/Category/UploadCategoryImage/{category.Id}",
            CreateMultipartImageContent("unsafe.svg", "image/svg+xml", "<svg></svg>"u8.ToArray()));
        using var fakePngResponse = await client.PostAsync(
            $"/Category/UploadCategoryImage/{category.Id}",
            CreateMultipartImageContent("fake.png", "image/png", "not an image"u8.ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, svgResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, fakePngResponse.StatusCode);
    }

    [Fact]
    public async Task UploadImage_ShouldRejectOversizedFile()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "image-oversized-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Image Oversized Cafe", "image-oversized-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Oversized Image", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var response = await client.PostAsync(
            $"/Category/UploadCategoryImage/{category.Id}",
            CreateMultipartImageContent("large.png", "image/png", new byte[(5 * 1024 * 1024) + 1]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceAndRemoveImage_ShouldCleanupOnlyManagedImages()
    {
        await using var factory = new CustomWebApplicationFactory();
        var owner = await SeedUserAsync(factory, "image-cleanup-owner@example.com");
        var cafe = await SeedCafeAsync(factory, "Image Cleanup Cafe", "image-cleanup-cafe");
        var category = await SeedCategoryAsync(factory, cafe.Id, "Cleanup Image", 1);
        await SeedMembershipAsync(factory, owner.Id, cafe.Id, ApplicationRoles.CafeOwner);
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, owner.Email);

        using var firstResponse = await client.PostAsync(
            $"/Category/UploadCategoryImage/{category.Id}",
            CreateMultipartImageContent("first.png", "image/png", CreateImageBytes(SKEncodedImageFormat.Png)));
        var firstImageUrl = (await ParseAsync(firstResponse)).RootElement.GetProperty("data").GetProperty("imageUrl").GetString();

        using var secondResponse = await client.PostAsync(
            $"/Category/UploadCategoryImage/{category.Id}",
            CreateMultipartImageContent("second.png", "image/png", CreateImageBytes(SKEncodedImageFormat.Png)));
        var secondImageUrl = (await ParseAsync(secondResponse)).RootElement.GetProperty("data").GetProperty("imageUrl").GetString();

        Assert.NotEqual(firstImageUrl, secondImageUrl);

        using var oldMediaResponse = await client.GetAsync(new Uri(firstImageUrl!).PathAndQuery);
        Assert.Equal(HttpStatusCode.NotFound, oldMediaResponse.StatusCode);

        await SetCategoryImageUrlAsync(factory, category.Id, "https://cdn.example.test/external.png");
        using var externalRemoveResponse = await client.PostAsync($"/Category/RemoveCategoryImage/{category.Id}", content: null);
        Assert.Equal(HttpStatusCode.OK, externalRemoveResponse.StatusCode);

        await SetCategoryImageUrlAsync(factory, category.Id, secondImageUrl);
        using var managedRemoveResponse = await client.PostAsync($"/Category/RemoveCategoryImage/{category.Id}", content: null);
        var removeJson = await ParseAsync(managedRemoveResponse);
        using var removedMediaResponse = await client.GetAsync(new Uri(secondImageUrl!).PathAndQuery);

        Assert.Equal(HttpStatusCode.OK, managedRemoveResponse.StatusCode);
        Assert.Equal(JsonValueKind.Null, removeJson.RootElement.GetProperty("data").GetProperty("imageUrl").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, removedMediaResponse.StatusCode);
    }

    [Fact]
    public async Task MediaEndpoint_ShouldRejectPathTraversalAndUnknownFolders()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var traversalResponse = await client.GetAsync("/media/categories/..%2Fsecret.png");
        using var unknownFolderResponse = await client.GetAsync("/media/unknown/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png");

        Assert.Equal(HttpStatusCode.NotFound, traversalResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownFolderResponse.StatusCode);
    }

    [Fact]
    public async Task PublicMenu_ShouldReturnStoredImageReferences()
    {
        await using var factory = new CustomWebApplicationFactory();
        var cafe = await SeedCafeAsync(factory, "Public Image Cafe", "public-image-cafe", isPublished: true);
        cafe.LogoImageUrl = "http://localhost/media/cafe-logos/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png";
        cafe.CoverImageUrl = "http://localhost/media/cafe-covers/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png";
        var category = await SeedCategoryAsync(factory, cafe.Id, "Public Images", 1, imageUrl: "http://localhost/media/categories/cccccccccccccccccccccccccccccccc.png");
        var product = await SeedProductAsync(factory, cafe.Id, category.Id, "Image Product", 1, 25m, imageUrl: "http://localhost/media/products/dddddddddddddddddddddddddddddddd.png");
        await SaveCafeAsync(factory, cafe);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/PublicMenu/GetMenu/{cafe.Slug}");
        var json = await ParseAsync(response);
        var data = json.RootElement.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(cafe.LogoImageUrl, data.GetProperty("logoImageUrl").GetString());
        Assert.Equal(cafe.CoverImageUrl, data.GetProperty("coverImageUrl").GetString());
        Assert.Equal(category.ImageUrl, data.GetProperty("categories")[0].GetProperty("imageUrl").GetString());
        Assert.Equal(product.ImageUrl, data.GetProperty("categories")[0].GetProperty("products")[0].GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task FailedDbUpdate_ShouldCleanupNewlyStoredManagedImage()
    {
        var category = new CategoryEntity
        {
            Id = 42,
            CafeId = 10,
            Name = "Cleanup",
            DisplayOrder = 1,
            IsVisible = true,
            IsPublished = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var cafe = new CafeEntity
        {
            Id = 10,
            Name = "Cleanup Cafe",
            Slug = "cleanup-cafe",
            IsActive = true,
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var imageStorage = new RecordingImageStorage();
        var service = new CategoryService(
            new StubCategoryRepository(category),
            new StubCafeRepository(cafe),
            new ThrowingUnitOfWork(),
            new NoOpTenantAuthorizationService(),
            imageStorage,
            new CategoryMapper(),
            NullLogger<CategoryService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadCategoryImageAsync(
            1,
            category.Id,
            new ImageUploadInput("image.png", "image/png", 4, new MemoryStream([1, 2, 3, 4])),
            CancellationToken.None));

        Assert.Equal("http://localhost/media/categories/eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee.png", imageStorage.StoredUrl);
        Assert.Contains(imageStorage.StoredUrl, imageStorage.DeletedUrls);
    }

    private static MultipartFormDataContent CreateMultipartImageContent(
        string fileName,
        string contentType,
        byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "File", fileName);
        return content;
    }

    private static byte[] CreateImageBytes(SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality: 90);

        Assert.NotNull(data);
        return data.ToArray();
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/Authentication/Login",
            new LoginRequest
            {
                Email = email,
                Password = ValidPassword
            });
        var json = await ParseAsync(response);
        var accessToken = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<AppUserEntity> SeedUserAsync(
        CustomWebApplicationFactory factory,
        string email,
        string? platformRoleCode = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        EnsureRoles(dbContext);

        var utcNow = DateTimeOffset.UtcNow;
        var user = new AppUserEntity
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = "Test User",
            PasswordHash = passwordHasher.HashPassword(ValidPassword),
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        if (platformRoleCode is not null)
        {
            var role = dbContext.Roles.Single(existingRole => existingRole.Code == platformRoleCode);
            user.Roles.Add(role);
        }

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task<CafeEntity> SeedCafeAsync(
        CustomWebApplicationFactory factory,
        string name,
        string slug,
        bool isActive = true,
        bool isPublished = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var cafe = new CafeEntity
        {
            Name = name,
            Slug = slug,
            IsActive = isActive,
            IsPublished = isPublished,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Cafes.Add(cafe);
        await dbContext.SaveChangesAsync();
        return cafe;
    }

    private static async Task<CategoryEntity> SeedCategoryAsync(
        CustomWebApplicationFactory factory,
        long cafeId,
        string name,
        int displayOrder,
        string? imageUrl = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var category = new CategoryEntity
        {
            CafeId = cafeId,
            Name = name,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder,
            IsVisible = true,
            IsPublished = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    private static async Task<ProductEntity> SeedProductAsync(
        CustomWebApplicationFactory factory,
        long cafeId,
        long categoryId,
        string name,
        int displayOrder,
        decimal price,
        string? imageUrl = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var product = new ProductEntity
        {
            CafeId = cafeId,
            CategoryId = categoryId,
            Name = name,
            Price = price,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder,
            IsAvailable = true,
            IsVisible = true,
            IsPublished = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    private static async Task SeedMembershipAsync(
        CustomWebApplicationFactory factory,
        long appUserId,
        long cafeId,
        string roleCode)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        EnsureRoles(dbContext);
        var role = dbContext.Roles.Single(existingRole => existingRole.Code == roleCode);
        var utcNow = DateTimeOffset.UtcNow;

        dbContext.CafeMemberships.Add(new CafeMembershipEntity
        {
            AppUserId = appUserId,
            CafeId = cafeId,
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SetCategoryImageUrlAsync(
        CustomWebApplicationFactory factory,
        long categoryId,
        string? imageUrl)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        var category = await dbContext.Categories.SingleAsync(existingCategory => existingCategory.Id == categoryId);
        category.ImageUrl = imageUrl;
        await dbContext.SaveChangesAsync();
    }

    private static async Task SaveCafeAsync(CustomWebApplicationFactory factory, CafeEntity cafe)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();
        dbContext.Cafes.Update(cafe);
        await dbContext.SaveChangesAsync();
    }

    private static void EnsureRoles(CafeMenuDbContext dbContext)
    {
        if (dbContext.Roles.Any())
        {
            return;
        }

        var utcNow = DateTimeOffset.UtcNow;
        dbContext.Roles.AddRange(
            new RoleEntity
            {
                Id = 1,
                Code = ApplicationRoles.PlatformAdmin,
                Name = "Platform Administrator",
                Description = "Manages platform-level administration.",
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            },
            new RoleEntity
            {
                Id = 2,
                Code = ApplicationRoles.CafeOwner,
                Name = "Cafe Owner",
                Description = "Cafe-scoped owner role reserved for membership authorization.",
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            },
            new RoleEntity
            {
                Id = 3,
                Code = ApplicationRoles.CafeManager,
                Name = "Cafe Manager",
                Description = "Cafe-scoped manager role reserved for membership authorization.",
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        dbContext.SaveChanges();
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    private sealed class RecordingImageStorage : IImageStorage
    {
        public string StoredUrl { get; } = "http://localhost/media/categories/eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee.png";

        public List<string> DeletedUrls { get; } = [];

        public Task<StoredImage> StoreAsync(
            ImageUploadInput input,
            ImageStorageFolder folder,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredImage(StoredUrl, "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee.png", "image/png"));
        }

        public Task DeleteIfManagedAsync(string? publicUrl, CancellationToken cancellationToken)
        {
            if (publicUrl is not null)
            {
                DeletedUrls.Add(publicUrl);
            }

            return Task.CompletedTask;
        }

        public Task<StoredImageFile?> GetAsync(
            ImageStorageFolder folder,
            string fileName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredImageFile?>(null);
        }

        public bool IsManagedUrl(string? publicUrl)
        {
            return string.Equals(publicUrl, StoredUrl, StringComparison.Ordinal);
        }
    }

    private sealed class StubCategoryRepository : ICategoryRepository
    {
        private readonly CategoryEntity _category;

        public StubCategoryRepository(CategoryEntity category)
        {
            _category = category;
        }

        public Task<CategoryEntity?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<CategoryEntity?>(id == _category.Id ? _category : null);
        }

        public Task<IReadOnlyCollection<CategoryEntity>> GetByCafeIdAsync(long cafeId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<CategoryEntity>>([]);
        }

        public Task<IReadOnlyCollection<CategoryEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<CategoryEntity>>([]);
        }

        public Task AddAsync(CategoryEntity category, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubCafeRepository : ICafeRepository
    {
        private readonly CafeEntity _cafe;

        public StubCafeRepository(CafeEntity cafe)
        {
            _cafe = cafe;
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<CafeEntity?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<CafeEntity?>(id == _cafe.Id ? _cafe : null);
        }

        public Task<CafeEntity?> GetByIdWithMembershipsAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<CafeEntity?>(id == _cafe.Id ? _cafe : null);
        }

        public Task<IReadOnlyCollection<CafeEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<CafeEntity>>([]);
        }

        public Task AddAsync(CafeEntity cafe, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated save failure.");
        }
    }

    private sealed class NoOpTenantAuthorizationService : ITenantAuthorizationService
    {
        public Task EnsureCafeAccessAsync(
            long appUserId,
            long cafeId,
            IReadOnlyCollection<string> allowedCafeRoles,
            bool allowPlatformAdmin,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
