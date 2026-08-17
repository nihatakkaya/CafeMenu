using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;

namespace CafeMenu.Api.Services;

public interface IAuthenticationService
{
    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);

    Task<UserResponseDto> GetCurrentUserAsync(long appUserId, CancellationToken cancellationToken);
}
