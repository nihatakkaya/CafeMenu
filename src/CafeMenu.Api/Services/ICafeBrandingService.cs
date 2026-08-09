using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;

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
}
