using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;

namespace CafeMenu.Api.Services;

public sealed class CategoryService : ICategoryService
{
    private static readonly string[] CategoryManagerRoles = [ApplicationRoles.CafeOwner, ApplicationRoles.CafeManager];

    private readonly ICategoryRepository _categoryRepository;
    private readonly ICafeRepository _cafeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly CategoryMapper _categoryMapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        ICafeRepository cafeRepository,
        IUnitOfWork unitOfWork,
        ITenantAuthorizationService tenantAuthorizationService,
        CategoryMapper categoryMapper,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _cafeRepository = cafeRepository;
        _unitOfWork = unitOfWork;
        _tenantAuthorizationService = tenantAuthorizationService;
        _categoryMapper = categoryMapper;
        _logger = logger;
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(
        long appUserId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCafeManagementAccessAsync(appUserId, request.CafeId, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var category = new CategoryEntity
        {
            CafeId = request.CafeId,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            ImageUrl = NormalizeOptionalText(request.ImageUrl),
            DisplayOrder = request.DisplayOrder,
            IsVisible = request.IsVisible,
            IsPublished = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} created for cafe {CafeId}", category.Id, category.CafeId);

        return _categoryMapper.ToResponse(category);
    }

    public async Task<CategoryResponseDto> GetCategoryByIdAsync(
        long appUserId,
        long categoryId,
        CancellationToken cancellationToken)
    {
        var category = await GetCategoryOrThrowAsync(categoryId, cancellationToken);
        await EnsureCafeManagementAccessAsync(appUserId, category.CafeId, cancellationToken);

        return _categoryMapper.ToResponse(category);
    }

    public async Task<IReadOnlyCollection<CategoryResponseDto>> GetCategoriesAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        await EnsureCafeManagementAccessAsync(appUserId, cafeId, cancellationToken);

        var categories = await _categoryRepository.GetByCafeIdAsync(cafeId, cancellationToken);
        return categories.Select(_categoryMapper.ToResponse).ToArray();
    }

    public async Task<CategoryResponseDto> UpdateCategoryAsync(
        long appUserId,
        long categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await GetCategoryOrThrowAsync(categoryId, cancellationToken);
        EnsureCategoryBelongsToCafe(category, request.CafeId);
        await EnsureCafeManagementAccessAsync(appUserId, category.CafeId, cancellationToken);

        category.Name = request.Name.Trim();
        category.Description = NormalizeOptionalText(request.Description);
        category.ImageUrl = NormalizeOptionalText(request.ImageUrl);
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category {CategoryId} updated for cafe {CafeId}", category.Id, category.CafeId);

        return _categoryMapper.ToResponse(category);
    }

    public async Task DeleteCategoryAsync(long appUserId, long categoryId, CancellationToken cancellationToken)
    {
        var category = await GetCategoryOrThrowAsync(categoryId, cancellationToken);
        await EnsureCafeManagementAccessAsync(appUserId, category.CafeId, cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        category.IsDeleted = true;
        category.DeletedAt = utcNow;
        category.UpdatedAt = utcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category {CategoryId} soft-deleted for cafe {CafeId}", category.Id, category.CafeId);
    }

    public async Task<CategoryResponseDto> ChangeCategoryVisibilityAsync(
        long appUserId,
        long categoryId,
        ChangeCategoryVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var category = await GetCategoryOrThrowAsync(categoryId, cancellationToken);
        EnsureCategoryBelongsToCafe(category, request.CafeId);
        await EnsureCafeManagementAccessAsync(appUserId, category.CafeId, cancellationToken);

        category.IsVisible = request.IsVisible;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Category {CategoryId} visibility changed for cafe {CafeId}",
            category.Id,
            category.CafeId);

        return _categoryMapper.ToResponse(category);
    }

    public async Task<IReadOnlyCollection<CategoryResponseDto>> ReorderCategoriesAsync(
        long appUserId,
        ReorderCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCafeManagementAccessAsync(appUserId, request.CafeId, cancellationToken);

        var requestedCategoryIds = request.Categories
            .Select(category => category.CategoryId)
            .Distinct()
            .ToArray();

        if (requestedCategoryIds.Length != request.Categories.Count)
        {
            throw new ConflictApplicationException(
                "Category reorder request contains duplicate category ids.",
                ApplicationErrorCodes.CategoryReorderInvalid);
        }

        var categories = await _categoryRepository.GetByIdsAsync(requestedCategoryIds, cancellationToken);

        if (categories.Count != requestedCategoryIds.Length)
        {
            throw new NotFoundApplicationException("Category was not found.", ApplicationErrorCodes.CategoryNotFound);
        }

        if (categories.Any(category => category.CafeId != request.CafeId))
        {
            throw new ForbiddenApplicationException(
                "Category does not belong to the requested cafe.",
                ApplicationErrorCodes.TenantAccessForbidden);
        }

        var ordersByCategoryId = request.Categories.ToDictionary(category => category.CategoryId, category => category.DisplayOrder);
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var category in categories)
        {
            category.DisplayOrder = ordersByCategoryId[category.Id];
            category.UpdatedAt = utcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Categories reordered for cafe {CafeId}", request.CafeId);

        return categories
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(_categoryMapper.ToResponse)
            .ToArray();
    }

    private async Task EnsureCafeManagementAccessAsync(long appUserId, long cafeId, CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            CategoryManagerRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe was not found.", ApplicationErrorCodes.CafeNotFound);

        if (!cafe.IsActive)
        {
            throw new ForbiddenApplicationException(
                "Cafe is not active for category management.",
                ApplicationErrorCodes.CafeInactive);
        }
    }

    private async Task<CategoryEntity> GetCategoryOrThrowAsync(long categoryId, CancellationToken cancellationToken)
    {
        return await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundApplicationException("Category was not found.", ApplicationErrorCodes.CategoryNotFound);
    }

    private static void EnsureCategoryBelongsToCafe(CategoryEntity category, long cafeId)
    {
        if (category.CafeId == cafeId)
        {
            return;
        }

        throw new ForbiddenApplicationException(
            "Category does not belong to the requested cafe.",
            ApplicationErrorCodes.TenantAccessForbidden);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
