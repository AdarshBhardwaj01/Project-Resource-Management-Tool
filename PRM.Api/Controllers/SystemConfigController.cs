using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;
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
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly EmailSettings _emailSettings;

    public SystemConfigController(
        ISystemConfigService systemConfigService,
        IEmailNotificationService emailNotificationService,
        IOptions<EmailSettings> emailSettings)
    {
        _systemConfigService = systemConfigService;
        _emailNotificationService = emailNotificationService;
        _emailSettings = emailSettings.Value;
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

    [HttpPost("test-email")]
    public async Task<ActionResult<ApiMessageResponse>> SendTestEmail(
        [FromBody] SendTestEmailRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_emailSettings.IsConfigured)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "EmailSettings is not configured. Add Email, Password, Host, and Port in appsettings."
            });
        }

        var toEmail = string.IsNullOrWhiteSpace(request?.ToEmail)
            ? _emailSettings.Email
            : request.ToEmail.Trim();

        await _emailNotificationService.SendTestEmailAsync(toEmail, cancellationToken);
        return Ok(new ApiMessageResponse
        {
            Message = $"Test email request processed for {toEmail}. Check inbox and API logs for delivery status."
        });
    }
}
