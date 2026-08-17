using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Security;
using CafeMenu.Api.Services;
using CafeMenu.Shared.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("PlatformUser")]
public sealed class PlatformUserController : ControllerBase
{
    private readonly IPlatformUserService _platformUserService;

    public PlatformUserController(IPlatformUserService platformUserService)
    {
        _platformUserService = platformUserService;
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpPost("CreateUserSetup")]
    [EnableRateLimiting(ApplicationRateLimitPolicyNames.PlatformUserSetup)]
    [ProducesResponseType(typeof(ApiResponse<UserSetupResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserSetupResponseDto>>> CreateUserSetup(
        [FromBody] CreateUserSetupRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _platformUserService.CreateUserSetupAsync(request, cancellationToken);
        return Created(string.Empty, ApiResponse<UserSetupResponseDto>.SuccessResponse(response, "User setup created successfully."));
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpGet("SearchUsers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<PlatformUserSearchResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PlatformUserSearchResponseDto>>>> SearchUsers(
        [FromQuery] SearchPlatformUsersRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _platformUserService.SearchUsersAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PlatformUserSearchResponseDto>>.SuccessResponse(response, "Users retrieved successfully."));
    }

    [AllowAnonymous]
    [HttpPost("CompleteUserSetup")]
    [EnableRateLimiting(ApplicationRateLimitPolicyNames.AccountSetup)]
    [ProducesResponseType(typeof(ApiResponse<PlatformUserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PlatformUserResponseDto>>> CompleteUserSetup(
        [FromBody] CompleteUserSetupRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _platformUserService.CompleteUserSetupAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformUserResponseDto>.SuccessResponse(response, "User setup completed successfully."));
    }

    [Authorize(Policy = ApplicationPolicies.PlatformAdministration)]
    [HttpPost("ReissueUserSetup/{userId:long}")]
    [EnableRateLimiting(ApplicationRateLimitPolicyNames.PlatformUserSetup)]
    [ProducesResponseType(typeof(ApiResponse<UserSetupResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserSetupResponseDto>>> ReissueUserSetup(
        long userId,
        CancellationToken cancellationToken)
    {
        var response = await _platformUserService.ReissueUserSetupAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserSetupResponseDto>.SuccessResponse(response, "User setup token reissued successfully."));
    }
}
