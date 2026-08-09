using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("CafeBranding")]
[Authorize]
public sealed class CafeBrandingController : ControllerBase
{
    private readonly ICafeBrandingService _cafeBrandingService;

    public CafeBrandingController(ICafeBrandingService cafeBrandingService)
    {
        _cafeBrandingService = cafeBrandingService;
    }

    [HttpGet("GetCafeBranding/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeBrandingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeBrandingResponseDto>>> GetCafeBranding(
        long cafeId,
        CancellationToken cancellationToken)
    {
        var response = await _cafeBrandingService.GetCafeBrandingAsync(GetCurrentAppUserId(), cafeId, cancellationToken);
        return Ok(ApiResponse<CafeBrandingResponseDto>.SuccessResponse(response, "Cafe branding retrieved successfully."));
    }

    [HttpPut("UpdateCafeBranding/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeBrandingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeBrandingResponseDto>>> UpdateCafeBranding(
        long cafeId,
        [FromBody] UpdateCafeBrandingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _cafeBrandingService.UpdateCafeBrandingAsync(GetCurrentAppUserId(), cafeId, request, cancellationToken);
        return Ok(ApiResponse<CafeBrandingResponseDto>.SuccessResponse(response, "Cafe branding updated successfully."));
    }

    private long GetCurrentAppUserId()
    {
        var appUserIdValue = User.FindFirstValue("app_user_id")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!long.TryParse(appUserIdValue, out var appUserId))
        {
            throw new UnauthorizedApplicationException("User is not authorized.", "AUTH004");
        }

        return appUserId;
    }
}
