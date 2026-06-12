using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Projects;

namespace PRM.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessageResponse>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _projectService.CreateProjectAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ProjectListResponse>> GetAllProjects(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _projectService.GetAllProjectsAsync(status, cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(int id, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.GetProjectAsync(id, cancellationToken);
            return Ok(project);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiMessageResponse>> UpdateProject(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _projectService.UpdateProjectAsync(id, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/milestones")]
    public async Task<ActionResult<ApiMessageResponse>> AddMilestone(
        int id,
        [FromBody] CreateMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _projectService.AddMilestoneAsync(id, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}/milestones/{milestoneId:int}")]
    public async Task<ActionResult<ApiMessageResponse>> UpdateMilestone(
        int id,
        int milestoneId,
        [FromBody] UpdateMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _projectService.UpdateMilestoneAsync(id, milestoneId, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }
}
