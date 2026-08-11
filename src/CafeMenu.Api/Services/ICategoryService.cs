using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;

namespace CafeMenu.Api.Services;

public interface ICategoryService
{
    Task<CategoryResponseDto> CreateCategoryAsync(
        long appUserId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<CategoryResponseDto> GetCategoryByIdAsync(long appUserId, long categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CategoryResponseDto>> GetCategoriesAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken);

    Task<CategoryResponseDto> UpdateCategoryAsync(
        long appUserId,
        long categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    Task DeleteCategoryAsync(long appUserId, long categoryId, CancellationToken cancellationToken);

    Task<CategoryResponseDto> ChangeCategoryVisibilityAsync(
        long appUserId,
        long categoryId,
        ChangeCategoryVisibilityRequest request,
        CancellationToken cancellationToken);

    Task<CategoryResponseDto> ChangeCategoryPublicationAsync(
        long appUserId,
        long categoryId,
        ChangeCategoryPublicationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CategoryResponseDto>> ReorderCategoriesAsync(
        long appUserId,
        ReorderCategoriesRequest request,
        CancellationToken cancellationToken);
}
