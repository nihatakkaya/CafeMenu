using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("System")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("Health")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SystemHealthResponse>> Health()
    {
        var response = new SystemHealthResponse(
            "Healthy",
            DateTimeOffset.UtcNow);

        return Ok(ApiResponse<SystemHealthResponse>.SuccessResponse(response, "Application is healthy."));
    }
}
