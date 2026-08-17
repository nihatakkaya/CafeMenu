using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Storage;

namespace CafeMenu.Api.Services;

public interface ICafeBrandingService
{
    Task<CafeBrandingResponseDto> GetCafeBrandingAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken);

    Task<CafeBrandingResponseDto> UpdateCafeBrandingAsync(
        long appUserId,
        long cafeId,
        UpdateCafeBrandingRequest request,
        CancellationToken cancellationToken);

    Task<CafeBrandingResponseDto> UploadLogoImageAsync(
        long appUserId,
        long cafeId,
        ImageUploadInput input,
        CancellationToken cancellationToken);

    Task<CafeBrandingResponseDto> UploadCoverImageAsync(
        long appUserId,
        long cafeId,
        ImageUploadInput input,
        CancellationToken cancellationToken);

    Task<CafeBrandingResponseDto> RemoveLogoImageAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken);

    Task<CafeBrandingResponseDto> RemoveCoverImageAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken);
}
