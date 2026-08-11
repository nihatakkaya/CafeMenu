using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;

namespace CafeMenu.Api.Services;

public interface IPlatformUserService
{
    Task<UserSetupResponseDto> CreateUserSetupAsync(
        CreateUserSetupRequest request,
        CancellationToken cancellationToken);

    Task<PlatformUserResponseDto> CompleteUserSetupAsync(
        CompleteUserSetupRequest request,
        CancellationToken cancellationToken);

    Task<UserSetupResponseDto> ReissueUserSetupAsync(
        long userId,
        CancellationToken cancellationToken);
}
