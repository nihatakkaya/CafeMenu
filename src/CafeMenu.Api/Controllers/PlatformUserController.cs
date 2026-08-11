using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Security;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [AllowAnonymous]
    [HttpPost("CompleteUserSetup")]
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
