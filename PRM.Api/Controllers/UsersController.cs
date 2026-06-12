using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Users;

namespace PRM.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessageResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _userService.CreateUserAsync(request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<UserListResponse>> GetAllUsers(CancellationToken cancellationToken)
    {
        var response = await _userService.GetAllUsersAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{usernameOrId}")]
    public async Task<ActionResult<UserDetailDto>> GetUser(
        string usernameOrId,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserAsync(usernameOrId, cancellationToken);
            return Ok(user);
        }
        catch (BusinessValidationException ex)
        {
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{usernameOrId}/reset-password")]
    public async Task<ActionResult<ApiMessageResponse>> ResetPassword(
        string usernameOrId,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _userService.ResetPasswordAsync(usernameOrId, request, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{usernameOrId}/deactivate")]
    public async Task<ActionResult<ApiMessageResponse>> DeactivateUser(
        string usernameOrId,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var message = await _userService.DeactivateUserAsync(usernameOrId, currentUserId, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    [HttpPut("{userId:int}/reactivate")]
    public async Task<ActionResult<ApiMessageResponse>> ReactivateUser(
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = await _userService.ReactivateUserAsync(userId, cancellationToken);
            return Ok(new ApiMessageResponse { Message = message });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
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
