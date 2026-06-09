using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.SystemConfig;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/system-config")]
[Authorize(Roles = "Admin")]
public class SystemConfigController : ControllerBase
{
    private readonly ISystemConfigService _systemConfigService;

    public SystemConfigController(ISystemConfigService systemConfigService)
    {
        _systemConfigService = systemConfigService;
    }

    [HttpGet]
    public async Task<ActionResult<SystemConfigDto>> GetSystemConfig(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _systemConfigService.GetSystemConfigAsync(cancellationToken);
            return Ok(config);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<ActionResult<ApiMessageResponse>> UpdateSystemConfig(
        [FromBody] UpdateSystemConfigRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _systemConfigService.UpdateSystemConfigAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }
}
