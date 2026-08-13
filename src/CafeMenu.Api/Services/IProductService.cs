using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Storage;

namespace CafeMenu.Api.Services;

public interface IProductService
{
    Task<ProductResponseDto> CreateProductAsync(long appUserId, CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponseDto> GetProductByIdAsync(long appUserId, long productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductResponseDto>> GetProductsAsync(
        long appUserId,
        long cafeId,
        long? categoryId,
        CancellationToken cancellationToken);

    Task<ProductResponseDto> UpdateProductAsync(
        long appUserId,
        long productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task DeleteProductAsync(long appUserId, long productId, CancellationToken cancellationToken);

    Task<ProductResponseDto> ChangeProductVisibilityAsync(
        long appUserId,
        long productId,
        ChangeProductVisibilityRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponseDto> ChangeProductAvailabilityAsync(
        long appUserId,
        long productId,
        ChangeProductAvailabilityRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponseDto> ChangeProductPublicationAsync(
        long appUserId,
        long productId,
        ChangeProductPublicationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductResponseDto>> ReorderProductsAsync(
        long appUserId,
        ReorderProductsRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponseDto> UploadProductImageAsync(
        long appUserId,
        long productId,
        ImageUploadInput input,
        CancellationToken cancellationToken);

    Task<ProductResponseDto> RemoveProductImageAsync(
        long appUserId,
        long productId,
        CancellationToken cancellationToken);
}
