using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("Authentication")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("Login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authenticationService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login completed successfully."));
    }

    [HttpPost("RefreshToken")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authenticationService.RefreshTokenAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(response, "Token refreshed successfully."));
    }

    [HttpPost("Logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authenticationService.LogoutAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Logout completed successfully."));
    }

    [Authorize]
    [HttpGet("GetCurrentUser")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var appUserIdValue = User.FindFirstValue("app_user_id")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!long.TryParse(appUserIdValue, out var appUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));
        }

        var response = await _authenticationService.GetCurrentUserAsync(appUserId, cancellationToken);
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(response, "Current user retrieved successfully."));
    }
}
