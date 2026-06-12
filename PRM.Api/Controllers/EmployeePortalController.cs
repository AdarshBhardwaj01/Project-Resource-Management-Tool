using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.EmployeePortal;

namespace PRM.Api.Controllers;
[ApiController]
[Route("api/employee")]
[Authorize(Roles = "Employee")]
public class EmployeePortalController : ControllerBase
{
    private readonly IEmployeePortalService _employeePortalService;

    public EmployeePortalController(IEmployeePortalService employeePortalService)
    {
        _employeePortalService = employeePortalService;
    }

    [HttpGet("allocations")]
    public async Task<ActionResult<IReadOnlyList<EmployeeAllocationItemDto>>> GetMyAllocations(
        CancellationToken cancellationToken)
    {
        try
        {
            var allocations = await _employeePortalService.GetMyAllocationsAsync(
                GetCurrentUserId(),
                cancellationToken);
            return Ok(allocations);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("timesheets/submit-preview")]
    public async Task<ActionResult<TimesheetSubmitPreviewResponse>> GetTimesheetSubmitPreview(
        [FromQuery] string? weekStart,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTime? weekStartDate = null;
            if (!string.IsNullOrWhiteSpace(weekStart))
            {
                weekStartDate = DateValidator.ParseRequired(weekStart, "Week start date");
            }
            var preview = await _employeePortalService.GetTimesheetSubmitPreviewAsync(
                GetCurrentUserId(),
                weekStartDate,
                cancellationToken);
            return Ok(preview);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("timesheets")]
    public async Task<ActionResult<ApiMessageResponse>> SubmitTimesheet(
        [FromBody] SubmitTimesheetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _employeePortalService.SubmitTimesheetAsync(
                GetCurrentUserId(),
                request,
                cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("timesheets")]
    public async Task<ActionResult<IReadOnlyList<EmployeeTimesheetHistoryItemDto>>> GetMyTimesheets(
        CancellationToken cancellationToken)
    {
        try
        {
            var timesheets = await _employeePortalService.GetMyTimesheetsAsync(
                GetCurrentUserId(),
                cancellationToken);
            return Ok(timesheets);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("timesheets/{timesheetId:int}")]
    public async Task<ActionResult<EmployeeTimesheetDetailDto>> GetTimesheetDetail(
        int timesheetId,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await _employeePortalService.GetTimesheetDetailAsync(
                GetCurrentUserId(),
                timesheetId,
                cancellationToken);
            return Ok(detail);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new BusinessValidationException("Invalid user session.");
        }
        return userId;
    }
}
