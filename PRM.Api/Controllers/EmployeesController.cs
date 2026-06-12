using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Resources;

namespace PRM.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EmployeesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public EmployeesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessageResponse>> CreateEmployee(
        [FromBody] CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.CreateResourceAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ResourceListResponse>> GetAllEmployees(
        [FromQuery] string? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _resourceService.GetAllResourcesAsync(status, department, cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResourceDetailDto>> GetEmployee(int id, CancellationToken cancellationToken)
    {
        try
        {
            var resource = await _resourceService.GetResourceAsync(id, cancellationToken);
            return Ok(resource);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiMessageResponse>> UpdateEmployee(
        int id,
        [FromBody] UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.UpdateResourceAsync(id, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<ActionResult<ApiMessageResponse>> DeactivateEmployee(int id, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.DeactivateResourceAsync(id, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("{id:int}/skills")]
    public async Task<ActionResult<IReadOnlyList<ResourceSkillDto>>> GetEmployeeSkills(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var skills = await _resourceService.GetResourceSkillsAsync(id, cancellationToken);
            return Ok(skills);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/skills")]
    public async Task<ActionResult<ApiMessageResponse>> AddEmployeeSkill(
        int id,
        [FromBody] AddResourceSkillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.AddResourceSkillAsync(id, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}/skills/{skillId:int}")]
    public async Task<ActionResult<ApiMessageResponse>> UpdateEmployeeSkill(
        int id,
        int skillId,
        [FromBody] UpdateResourceSkillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.UpdateResourceSkillAsync(id, skillId, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/skills/{skillId:int}")]
    public async Task<ActionResult<ApiMessageResponse>> RemoveEmployeeSkill(
        int id,
        int skillId,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.RemoveResourceSkillAsync(id, skillId, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("assign-manager")]
    public async Task<ActionResult<ApiMessageResponse>> AssignManager(
        [FromBody] AssignManagerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _resourceService.AssignManagerAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }
}
