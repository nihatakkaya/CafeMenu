namespace CafeMenu.Web.AdminCategory;

public interface IAdminCategoryApiClient
{
    Task<AdminCategoryListResult> GetCategoriesAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminCategoryMutationResult> CreateCategoryAsync(
        AdminCreateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<AdminCategoryMutationResult> UpdateCategoryAsync(
        long categoryId,
        AdminUpdateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<AdminCategoryDeleteResult> DeleteCategoryAsync(long categoryId, CancellationToken cancellationToken);

    Task<AdminCategoryMutationResult> ChangeCategoryVisibilityAsync(
        long categoryId,
        AdminChangeCategoryVisibilityRequest request,
        CancellationToken cancellationToken);

    Task<AdminCategoryListResult> ReorderCategoriesAsync(
        AdminReorderCategoriesRequest request,
        CancellationToken cancellationToken);
}
