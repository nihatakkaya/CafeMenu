using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Storage;

namespace CafeMenu.Api.Services;

public sealed class ProductService : IProductService
{
    private static readonly string[] ProductManagerRoles = [ApplicationRoles.CafeOwner, ApplicationRoles.CafeManager];

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICafeRepository _cafeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly IImageStorage _imageStorage;
    private readonly ProductMapper _productMapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ICafeRepository cafeRepository,
        IUnitOfWork unitOfWork,
        ITenantAuthorizationService tenantAuthorizationService,
        IImageStorage imageStorage,
        ProductMapper productMapper,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _cafeRepository = cafeRepository;
        _unitOfWork = unitOfWork;
        _tenantAuthorizationService = tenantAuthorizationService;
        _imageStorage = imageStorage;
        _productMapper = productMapper;
        _logger = logger;
    }

    public async Task<ProductResponseDto> CreateProductAsync(
        long appUserId,
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCafeProductManagementAccessAsync(appUserId, request.CafeId, cancellationToken);
        await EnsureCategoryBelongsToCafeAsync(request.CategoryId, request.CafeId, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var product = new ProductEntity
        {
            CafeId = request.CafeId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            Price = request.Price,
            ImageUrl = NormalizeOptionalText(request.ImageUrl),
            IsAvailable = request.IsAvailable,
            IsVisible = request.IsVisible,
            IsPublished = false,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} created for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    public async Task<ProductResponseDto> GetProductByIdAsync(
        long appUserId,
        long productId,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        return _productMapper.ToResponse(product);
    }

    public async Task<IReadOnlyCollection<ProductResponseDto>> GetProductsAsync(
        long appUserId,
        long cafeId,
        long? categoryId,
        CancellationToken cancellationToken)
    {
        await EnsureCafeProductManagementAccessAsync(appUserId, cafeId, cancellationToken);

        if (categoryId.HasValue)
        {
            await EnsureCategoryBelongsToCafeAsync(categoryId.Value, cafeId, cancellationToken);
        }

        var products = await _productRepository.GetByCafeIdAsync(cafeId, categoryId, cancellationToken);
        return products.Select(_productMapper.ToResponse).ToArray();
    }

    public async Task<ProductResponseDto> UpdateProductAsync(
        long appUserId,
        long productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        EnsureProductBelongsToCafe(product, request.CafeId);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);
        await EnsureCategoryBelongsToCafeAsync(request.CategoryId, request.CafeId, cancellationToken);

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = NormalizeOptionalText(request.Description);
        product.Price = request.Price;
        product.ImageUrl = NormalizeOptionalText(request.ImageUrl);
        product.DisplayOrder = request.DisplayOrder;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product {ProductId} updated for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    public async Task DeleteProductAsync(long appUserId, long productId, CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        product.IsDeleted = true;
        product.DeletedAt = utcNow;
        product.UpdatedAt = utcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product {ProductId} soft-deleted for cafe {CafeId}", product.Id, product.CafeId);
    }

    public async Task<ProductResponseDto> ChangeProductVisibilityAsync(
        long appUserId,
        long productId,
        ChangeProductVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        EnsureProductBelongsToCafe(product, request.CafeId);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        product.IsVisible = request.IsVisible;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product {ProductId} visibility changed for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    public async Task<ProductResponseDto> ChangeProductAvailabilityAsync(
        long appUserId,
        long productId,
        ChangeProductAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        EnsureProductBelongsToCafe(product, request.CafeId);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product {ProductId} availability changed for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    public async Task<ProductResponseDto> ChangeProductPublicationAsync(
        long appUserId,
        long productId,
        ChangeProductPublicationRequest request,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        EnsureProductBelongsToCafe(product, request.CafeId);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        product.IsPublished = request.IsPublished;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product {ProductId} publication changed for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    public async Task<IReadOnlyCollection<ProductResponseDto>> ReorderProductsAsync(
        long appUserId,
        ReorderProductsRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCafeProductManagementAccessAsync(appUserId, request.CafeId, cancellationToken);
        await EnsureCategoryBelongsToCafeAsync(request.CategoryId, request.CafeId, cancellationToken);

        var requestedProductIds = request.Products
            .Select(product => product.ProductId)
            .Distinct()
            .ToArray();

        if (requestedProductIds.Length != request.Products.Count)
        {
            throw new ConflictApplicationException(
                "Product reorder request contains duplicate product ids.",
                ApplicationErrorCodes.ProductReorderInvalid);
        }

        var products = await _productRepository.GetByIdsAsync(requestedProductIds, cancellationToken);

        if (products.Count != requestedProductIds.Length)
        {
            throw new NotFoundApplicationException("Product was not found.", ApplicationErrorCodes.ProductNotFound);
        }

        if (products.Any(product => product.CafeId != request.CafeId || product.CategoryId != request.CategoryId))
        {
            throw new ForbiddenApplicationException(
                "Product does not belong to the requested cafe and category.",
                ApplicationErrorCodes.TenantAccessForbidden);
        }

        var ordersByProductId = request.Products.ToDictionary(product => product.ProductId, product => product.DisplayOrder);
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var product in products)
        {
            product.DisplayOrder = ordersByProductId[product.Id];
            product.UpdatedAt = utcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Products reordered for cafe {CafeId} category {CategoryId}", request.CafeId, request.CategoryId);

        return products
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .Select(_productMapper.ToResponse)
            .ToArray();
    }

    public async Task<ProductResponseDto> UploadProductImageAsync(
        long appUserId,
        long productId,
        ImageUploadInput input,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        var oldImageUrl = product.ImageUrl;
        StoredImage? storedImage = null;

        try
        {
            storedImage = await _imageStorage.StoreAsync(input, ImageStorageFolder.Products, cancellationToken);
            product.ImageUrl = storedImage.PublicUrl;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (storedImage is not null)
            {
                await TryDeleteManagedImageAsync(storedImage.PublicUrl, cancellationToken);
            }

            throw;
        }

        await TryDeleteManagedImageAsync(oldImageUrl, cancellationToken);
        _logger.LogInformation("Product {ProductId} image uploaded for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    public async Task<ProductResponseDto> RemoveProductImageAsync(
        long appUserId,
        long productId,
        CancellationToken cancellationToken)
    {
        var product = await GetProductOrThrowAsync(productId, cancellationToken);
        await EnsureCafeProductManagementAccessAsync(appUserId, product.CafeId, cancellationToken);

        var oldImageUrl = product.ImageUrl;
        product.ImageUrl = null;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryDeleteManagedImageAsync(oldImageUrl, cancellationToken);
        _logger.LogInformation("Product {ProductId} image removed for cafe {CafeId}", product.Id, product.CafeId);

        return _productMapper.ToResponse(product);
    }

    private async Task EnsureCafeProductManagementAccessAsync(long appUserId, long cafeId, CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            ProductManagerRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe was not found.", ApplicationErrorCodes.CafeNotFound);

        if (!cafe.IsActive)
        {
            throw new ForbiddenApplicationException(
                "Cafe is not active for product management.",
                ApplicationErrorCodes.CafeInactive);
        }
    }

    private async Task<CategoryEntity> EnsureCategoryBelongsToCafeAsync(
        long categoryId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundApplicationException("Category was not found.", ApplicationErrorCodes.CategoryNotFound);

        if (category.CafeId == cafeId)
        {
            return category;
        }

        throw new ForbiddenApplicationException(
            "Category does not belong to the requested cafe.",
            ApplicationErrorCodes.ProductInvalidCategoryRelationship);
    }

    private async Task<ProductEntity> GetProductOrThrowAsync(long productId, CancellationToken cancellationToken)
    {
        return await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundApplicationException("Product was not found.", ApplicationErrorCodes.ProductNotFound);
    }

    private static void EnsureProductBelongsToCafe(ProductEntity product, long cafeId)
    {
        if (product.CafeId == cafeId)
        {
            return;
        }

        throw new ForbiddenApplicationException(
            "Product does not belong to the requested cafe.",
            ApplicationErrorCodes.TenantAccessForbidden);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task TryDeleteManagedImageAsync(string? imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorage.DeleteIfManagedAsync(imageUrl, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Managed product image cleanup failed.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Managed product image cleanup failed.");
        }
    }
}
