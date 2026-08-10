namespace CafeMenu.Web.AdminProduct;

public interface IAdminProductApiClient
{
    Task<AdminProductListResult> GetProductsAsync(long cafeId, CancellationToken cancellationToken);

    Task<AdminProductMutationResult> CreateProductAsync(
        AdminCreateProductRequest request,
        CancellationToken cancellationToken);

    Task<AdminProductMutationResult> UpdateProductAsync(
        long productId,
        AdminUpdateProductRequest request,
        CancellationToken cancellationToken);

    Task<AdminProductDeleteResult> DeleteProductAsync(long productId, CancellationToken cancellationToken);

    Task<AdminProductMutationResult> ChangeProductVisibilityAsync(
        long productId,
        AdminChangeProductVisibilityRequest request,
        CancellationToken cancellationToken);

    Task<AdminProductMutationResult> ChangeProductAvailabilityAsync(
        long productId,
        AdminChangeProductAvailabilityRequest request,
        CancellationToken cancellationToken);

    Task<AdminProductListResult> ReorderProductsAsync(
        AdminReorderProductsRequest request,
        CancellationToken cancellationToken);
}
