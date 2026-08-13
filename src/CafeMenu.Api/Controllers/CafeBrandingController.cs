using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Services;
using CafeMenu.Api.Storage;
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

    [HttpPost("UploadLogoImage/{cafeId:long}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CafeBrandingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeBrandingResponseDto>>> UploadLogoImage(
        long cafeId,
        [FromForm] ImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = OpenImageStream(request);
        var response = await _cafeBrandingService.UploadLogoImageAsync(
            GetCurrentAppUserId(),
            cafeId,
            ToImageUploadInput(request.File!, stream),
            cancellationToken);

        return Ok(ApiResponse<CafeBrandingResponseDto>.SuccessResponse(response, "Cafe logo image uploaded successfully."));
    }

    [HttpPost("UploadCoverImage/{cafeId:long}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CafeBrandingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeBrandingResponseDto>>> UploadCoverImage(
        long cafeId,
        [FromForm] ImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = OpenImageStream(request);
        var response = await _cafeBrandingService.UploadCoverImageAsync(
            GetCurrentAppUserId(),
            cafeId,
            ToImageUploadInput(request.File!, stream),
            cancellationToken);

        return Ok(ApiResponse<CafeBrandingResponseDto>.SuccessResponse(response, "Cafe cover image uploaded successfully."));
    }

    [HttpPost("RemoveLogoImage/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeBrandingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeBrandingResponseDto>>> RemoveLogoImage(
        long cafeId,
        CancellationToken cancellationToken)
    {
        var response = await _cafeBrandingService.RemoveLogoImageAsync(GetCurrentAppUserId(), cafeId, cancellationToken);
        return Ok(ApiResponse<CafeBrandingResponseDto>.SuccessResponse(response, "Cafe logo image removed successfully."));
    }

    [HttpPost("RemoveCoverImage/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeBrandingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeBrandingResponseDto>>> RemoveCoverImage(
        long cafeId,
        CancellationToken cancellationToken)
    {
        var response = await _cafeBrandingService.RemoveCoverImageAsync(GetCurrentAppUserId(), cafeId, cancellationToken);
        return Ok(ApiResponse<CafeBrandingResponseDto>.SuccessResponse(response, "Cafe cover image removed successfully."));
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

    private static Stream OpenImageStream(ImageUploadRequest request)
    {
        return request.File is null
            ? throw new BadRequestApplicationException("Image file is required.", ApplicationErrorCodes.ImageInvalid)
            : request.File.OpenReadStream();
    }

    private static ImageUploadInput ToImageUploadInput(IFormFile file, Stream stream)
    {
        return new ImageUploadInput(file.FileName, file.ContentType, file.Length, stream);
    }
}
