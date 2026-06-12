using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Allocations;
using PRM.Models.DTOs.Auth;

namespace PRM.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationsController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet]
    public async Task<ActionResult<AllocationListResponse>> GetAllAllocations(
        [FromQuery] int? employeeId,
        [FromQuery] int? projectId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _allocationService.GetAllAllocationsAsync(
                employeeId,
                projectId,
                status,
                cancellationToken);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }
}
