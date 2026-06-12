using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Manager;

namespace PRM.Api.Controllers;
[ApiController]
[Route("api/manager")]
[Authorize(Roles = "Manager")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagerController(IManagerService managerService)
    {
        _managerService = managerService;
    }

    [HttpGet("resource-dashboard")]
    public async Task<ActionResult<ResourceDashboardResponse>> GetResourceDashboard(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _managerService.GetResourceDashboardAsync(GetCurrentUserId(), cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<ManagerProjectItemDto>>> GetMyProjects(
        CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _managerService.GetMyProjectsAsync(GetCurrentUserId(), cancellationToken);
            return Ok(projects);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("projects/{projectId:int}")]
    public async Task<ActionResult<ManagerProjectDetailDto>> GetMyProjectDetail(
        int projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var project = await _managerService.GetMyProjectDetailAsync(
                GetCurrentUserId(),
                projectId,
                cancellationToken);
            return Ok(project);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("allocations")]
    public async Task<ActionResult<ApiMessageResponse>> AllocateResource(
        [FromBody] CreateAllocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _managerService.AllocateResourceAsync(
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

    [HttpGet("employees/{id:int}")]
    public async Task<ActionResult<EmployeeDrillDownDto>> GetEmployeeDrillDown(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _managerService.GetEmployeeDrillDownAsync(GetCurrentUserId(), id, cancellationToken);
            return Ok(employee);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("employees/{id:int}/utilisation-preview")]
    public async Task<ActionResult<EmployeeUtilisationPreviewDto>> GetEmployeeUtilisationPreview(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _managerService.GetEmployeeUtilisationPreviewAsync(
                GetCurrentUserId(),
                id,
                cancellationToken);
            return Ok(preview);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("allocations/validate")]
    public async Task<ActionResult<AllocationValidationDto>> ValidateAllocation(
        [FromBody] CreateAllocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validation = await _managerService.ValidateAllocationAsync(
                GetCurrentUserId(),
                request,
                cancellationToken);
            return Ok(validation);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("projects/{projectId:int}/allocations")]
    public async Task<ActionResult<IReadOnlyList<ProjectAllocationListItemDto>>> GetProjectActiveAllocations(
        int projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var allocations = await _managerService.GetProjectActiveAllocationsAsync(
                GetCurrentUserId(),
                projectId,
                cancellationToken);
            return Ok(allocations);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("allocations/{id:int}/end")]
    public async Task<ActionResult<ApiMessageResponse>> EndAllocation(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _managerService.EndAllocationAsync(
                GetCurrentUserId(),
                id,
                cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("timesheets")]
    public async Task<ActionResult<ManagerTeamTimesheetsResponse>> GetTeamTimesheets(
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
            var response = await _managerService.GetTeamTimesheetsAsync(
                GetCurrentUserId(),
                weekStartDate,
                cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("timesheets/employees/{employeeId:int}")]
    public async Task<ActionResult<ManagerEmployeeTimesheetDetailDto>> GetEmployeeTimesheetDetail(
        int employeeId,
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
            var response = await _managerService.GetEmployeeTimesheetDetailAsync(
                GetCurrentUserId(),
                employeeId,
                weekStartDate,
                cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("timesheets/frozen")]
    public async Task<ActionResult<IReadOnlyList<FrozenTimesheetItemDto>>> GetFrozenTimesheets(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _managerService.GetFrozenTimesheetsAsync(
                GetCurrentUserId(),
                cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("timesheets/frozen/restore")]
    public async Task<ActionResult<ApiMessageResponse>> RestoreFrozenTimesheet(
        [FromBody] RestoreFrozenTimesheetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _managerService.RestoreFrozenTimesheetAsync(
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

    [HttpPost("ai/skill-match")]
    public async Task<ActionResult<SkillMatchResponse>> GetSkillMatch(
        [FromBody] SkillMatchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _managerService.GetSkillMatchAsync(
                GetCurrentUserId(),
                request,
                cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("ai/team-build")]
    public async Task<ActionResult<TeamBuildResponse>> BuildTeam(
        [FromBody] TeamBuildRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _managerService.BuildTeamAsync(
                GetCurrentUserId(),
                request,
                cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("ai/projects/{projectId:int}/risk-summary")]
    public async Task<ActionResult<ProjectRiskSummaryResponse>> GetProjectRiskSummary(
        int projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _managerService.GetProjectRiskSummaryAsync(
                GetCurrentUserId(),
                projectId,
                cancellationToken);
            return Ok(response);
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
