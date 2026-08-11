using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;

namespace CafeMenu.Api.Services;

public interface ICafeService
{
    Task<CafeResponseDto> CreateCafeAsync(CreateCafeRequest request, CancellationToken cancellationToken);

    Task<CafeDetailResponseDto> GetCafeByIdAsync(long appUserId, long cafeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CafeResponseDto>> GetCafesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MyCafeResponseDto>> GetMyCafesAsync(long appUserId, CancellationToken cancellationToken);

    Task<CafeResponseDto> UpdateCafeAsync(long appUserId, long cafeId, UpdateCafeRequest request, CancellationToken cancellationToken);

    Task<CafeResponseDto> ChangeCafePublicationAsync(
        long appUserId,
        long cafeId,
        ChangeCafePublicationRequest request,
        CancellationToken cancellationToken);

    Task<CafeResponseDto> ActivateCafeAsync(long cafeId, CancellationToken cancellationToken);

    Task<CafeResponseDto> DeactivateCafeAsync(long cafeId, CancellationToken cancellationToken);

    Task<CafeMembershipResponseDto> AssignCafeOwnerAsync(AssignCafeOwnerRequest request, CancellationToken cancellationToken);

    Task<CafeMembershipResponseDto> AssignCafeManagerAsync(
        long appUserId,
        AssignCafeManagerRequest request,
        CancellationToken cancellationToken);

    Task<CafeMembershipResponseDto> DeactivateCafeMembershipAsync(
        long appUserId,
        long membershipId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CafeMemberResponseDto>> GetCafeMembersAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken);
}
