using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Security;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("Cafe")]
[Authorize]
public sealed class CafeController : ControllerBase
{
    private readonly ICafeService _cafeService;

    public CafeController(ICafeService cafeService)
    {
        _cafeService = cafeService;
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpPost("CreateCafe")]
    [ProducesResponseType(typeof(ApiResponse<CafeResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CafeResponseDto>>> CreateCafe(
        [FromBody] CreateCafeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.CreateCafeAsync(request, cancellationToken);
        return Created(string.Empty, ApiResponse<CafeResponseDto>.SuccessResponse(response, "Cafe created successfully."));
    }

    [HttpGet("GetCafeById/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeDetailResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeDetailResponseDto>>> GetCafeById(
        long id,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.GetCafeByIdAsync(GetCurrentAppUserId(), id, cancellationToken);
        return Ok(ApiResponse<CafeDetailResponseDto>.SuccessResponse(response, "Cafe retrieved successfully."));
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpGet("GetCafes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CafeResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CafeResponseDto>>>> GetCafes(CancellationToken cancellationToken)
    {
        var response = await _cafeService.GetCafesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CafeResponseDto>>.SuccessResponse(response, "Cafes retrieved successfully."));
    }

    [HttpGet("GetMyCafes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<MyCafeResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MyCafeResponseDto>>>> GetMyCafes(CancellationToken cancellationToken)
    {
        var response = await _cafeService.GetMyCafesAsync(GetCurrentAppUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<MyCafeResponseDto>>.SuccessResponse(response, "Accessible cafes retrieved successfully."));
    }

    [HttpPut("UpdateCafe/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CafeResponseDto>>> UpdateCafe(
        long id,
        [FromBody] UpdateCafeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.UpdateCafeAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<CafeResponseDto>.SuccessResponse(response, "Cafe updated successfully."));
    }

    [HttpPut("ChangeCafePublication/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeResponseDto>>> ChangeCafePublication(
        long id,
        [FromBody] ChangeCafePublicationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.ChangeCafePublicationAsync(GetCurrentAppUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<CafeResponseDto>.SuccessResponse(response, "Cafe publication changed successfully."));
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpPut("ActivateCafe/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeResponseDto>>> ActivateCafe(
        long id,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.ActivateCafeAsync(id, cancellationToken);
        return Ok(ApiResponse<CafeResponseDto>.SuccessResponse(response, "Cafe activated successfully."));
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpPut("DeactivateCafe/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeResponseDto>>> DeactivateCafe(
        long id,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.DeactivateCafeAsync(id, cancellationToken);
        return Ok(ApiResponse<CafeResponseDto>.SuccessResponse(response, "Cafe deactivated successfully."));
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpPost("AssignCafeOwner")]
    [ProducesResponseType(typeof(ApiResponse<CafeMembershipResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CafeMembershipResponseDto>>> AssignCafeOwner(
        [FromBody] AssignCafeOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.AssignCafeOwnerAsync(request, cancellationToken);
        return Ok(ApiResponse<CafeMembershipResponseDto>.SuccessResponse(response, "Cafe owner assigned successfully."));
    }

    [HttpPost("AssignCafeManager")]
    [ProducesResponseType(typeof(ApiResponse<CafeMembershipResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CafeMembershipResponseDto>>> AssignCafeManager(
        [FromBody] AssignCafeManagerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.AssignCafeManagerAsync(GetCurrentAppUserId(), request, cancellationToken);
        return Ok(ApiResponse<CafeMembershipResponseDto>.SuccessResponse(response, "Cafe manager assigned successfully."));
    }

    [HttpPost("DeactivateCafeMembership/{membershipId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CafeMembershipResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CafeMembershipResponseDto>>> DeactivateCafeMembership(
        long membershipId,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.DeactivateCafeMembershipAsync(GetCurrentAppUserId(), membershipId, cancellationToken);
        return Ok(ApiResponse<CafeMembershipResponseDto>.SuccessResponse(response, "Cafe membership deactivated successfully."));
    }

    [HttpGet("GetCafeMembers/{cafeId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CafeMemberResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CafeMemberResponseDto>>>> GetCafeMembers(
        long cafeId,
        CancellationToken cancellationToken)
    {
        var response = await _cafeService.GetCafeMembersAsync(GetCurrentAppUserId(), cafeId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CafeMemberResponseDto>>.SuccessResponse(response, "Cafe members retrieved successfully."));
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
