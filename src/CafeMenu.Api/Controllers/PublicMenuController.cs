using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("PublicMenu")]
[AllowAnonymous]
public sealed class PublicMenuController : ControllerBase
{
    private readonly IPublicMenuService _publicMenuService;

    public PublicMenuController(IPublicMenuService publicMenuService)
    {
        _publicMenuService = publicMenuService;
    }

    [HttpGet("GetMenu/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<PublicMenuResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PublicMenuResponseDto>>> GetMenu(
        string slug,
        CancellationToken cancellationToken)
    {
        var response = await _publicMenuService.GetMenuAsync(slug, cancellationToken);
        return Ok(ApiResponse<PublicMenuResponseDto>.SuccessResponse(response, "Public menu retrieved successfully."));
    }

    [HttpGet("GetProductDetail/{slug}/{productId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PublicMenuProductDetailResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PublicMenuProductDetailResponseDto>>> GetProductDetail(
        string slug,
        long productId,
        CancellationToken cancellationToken)
    {
        var response = await _publicMenuService.GetProductDetailAsync(slug, productId, cancellationToken);
        return Ok(ApiResponse<PublicMenuProductDetailResponseDto>.SuccessResponse(response, "Public product retrieved successfully."));
    }
}
