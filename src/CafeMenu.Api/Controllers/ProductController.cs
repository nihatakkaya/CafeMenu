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
[Route("Product")]
[Authorize]
public sealed class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost("CreateProduct")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _productService.CreateProductAsync(GetCurrentAppUserId(), request, cancellationToken);
        return Created(string.Empty, ApiResponse<ProductResponseDto>.SuccessResponse(response, "Product created successfully."));
    }

    [HttpGet("GetProductById/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> GetProductById(
        long id,
        CancellationToken cancellationToken)
    {
        var response = await _productService.GetProductByIdAsync(GetCurrentAppUserId(), id, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(response, "Product retrieved successfully."));
    }

    [HttpGet("GetProducts/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductResponseDto>>>> GetProducts(
        long cafeId,
        [FromQuery] long? categoryId,
        CancellationToken cancellationToken)
    {
        var response = await _productService.GetProductsAsync(GetCurrentAppUserId(), cafeId, categoryId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ProductResponseDto>>.SuccessResponse(response, "Products retrieved successfully."));
    }

    [HttpPut("UpdateProduct/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> UpdateProduct(
        long id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _productService.UpdateProductAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(response, "Product updated successfully."));
    }

    [HttpDelete("DeleteProduct/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(long id, CancellationToken cancellationToken)
    {
        await _productService.DeleteProductAsync(GetCurrentAppUserId(), id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Product deleted successfully."));
    }

    [HttpPut("ChangeProductVisibility/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> ChangeProductVisibility(
        long id,
        [FromBody] ChangeProductVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _productService.ChangeProductVisibilityAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(response, "Product visibility changed successfully."));
    }

    [HttpPut("ChangeProductAvailability/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> ChangeProductAvailability(
        long id,
        [FromBody] ChangeProductAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _productService.ChangeProductAvailabilityAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(response, "Product availability changed successfully."));
    }

    [HttpPut("ChangeProductPublication/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDto>>> ChangeProductPublication(
        long id,
        [FromBody] ChangeProductPublicationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _productService.ChangeProductPublicationAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<ProductResponseDto>.SuccessResponse(response, "Product publication changed successfully."));
    }

    [HttpPut("ReorderProducts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductResponseDto>>>> ReorderProducts(
        [FromBody] ReorderProductsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _productService.ReorderProductsAsync(GetCurrentAppUserId(), request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ProductResponseDto>>.SuccessResponse(response, "Products reordered successfully."));
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
