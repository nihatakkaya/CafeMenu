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
[Route("Category")]
[Authorize]
public sealed class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost("CreateCategory")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.CreateCategoryAsync(GetCurrentAppUserId(), request, cancellationToken);
        return Created(string.Empty, ApiResponse<CategoryResponseDto>.SuccessResponse(response, "Category created successfully."));
    }

    [HttpGet("GetCategoryById/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetCategoryById(
        long id,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.GetCategoryByIdAsync(GetCurrentAppUserId(), id, cancellationToken);
        return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(response, "Category retrieved successfully."));
    }

    [HttpGet("GetCategories/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CategoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CategoryResponseDto>>>> GetCategories(
        long cafeId,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.GetCategoriesAsync(GetCurrentAppUserId(), cafeId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CategoryResponseDto>>.SuccessResponse(response, "Categories retrieved successfully."));
    }

    [HttpPut("UpdateCategory/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> UpdateCategory(
        long id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.UpdateCategoryAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(response, "Category updated successfully."));
    }

    [HttpDelete("DeleteCategory/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(long id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteCategoryAsync(GetCurrentAppUserId(), id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Category deleted successfully."));
    }

    [HttpPut("ChangeCategoryVisibility/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> ChangeCategoryVisibility(
        long id,
        [FromBody] ChangeCategoryVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.ChangeCategoryVisibilityAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(response, "Category visibility changed successfully."));
    }

    [HttpPut("ReorderCategories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CategoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CategoryResponseDto>>>> ReorderCategories(
        [FromBody] ReorderCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.ReorderCategoriesAsync(GetCurrentAppUserId(), request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CategoryResponseDto>>.SuccessResponse(response, "Categories reordered successfully."));
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
