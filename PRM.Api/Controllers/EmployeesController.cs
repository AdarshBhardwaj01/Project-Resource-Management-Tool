using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Employees;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessageResponse>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _employeeService.CreateEmployeeAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeListResponse>> GetAllEmployees(
        [FromQuery] string? status,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _employeeService.GetAllEmployeesAsync(status, department, cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetEmployee(int id, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeAsync(id, cancellationToken);
            return Ok(employee);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiMessageResponse>> UpdateEmployee(
        int id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
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
            var message = await _employeeService.DeactivateEmployeeAsync(id, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet("{id:int}/skills")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSkillDto>>> GetEmployeeSkills(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var skills = await _employeeService.GetEmployeeSkillsAsync(id, cancellationToken);
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
        [FromBody] AddEmployeeSkillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _employeeService.AddEmployeeSkillAsync(id, request, cancellationToken);
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
        [FromBody] UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _employeeService.UpdateEmployeeSkillAsync(id, skillId, request, cancellationToken);
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
            var message = await _employeeService.RemoveEmployeeSkillAsync(id, skillId, cancellationToken);
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
            var message = await _employeeService.AssignManagerAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }
}
