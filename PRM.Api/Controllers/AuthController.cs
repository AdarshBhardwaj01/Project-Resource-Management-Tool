using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using PRM.Business.Interfaces.Services;

using PRM.Common.Exceptions;

using PRM.Models.DTOs.Auth;



namespace PRM.Api.Controllers;



[ApiController]

[Route("api/[controller]")]

public class AuthController : ControllerBase

{

    private readonly IAuthService _authService;



    public AuthController(IAuthService authService)

    {

        _authService = authService;

    }



    [HttpPost("login")]

    [AllowAnonymous]

    public async Task<ActionResult<LoginResponse>> Login(

        [FromBody] LoginRequest request,

        CancellationToken cancellationToken)

    {

        try

        {

            var response = await _authService.LoginAsync(request, cancellationToken);

            return Ok(response);

        }

        catch (BusinessValidationException ex)

        {

            return Unauthorized(new ApiErrorResponse { Message = ex.Message });

        }

    }



    [HttpPost("change-password")]

    [Authorize]

    public async Task<ActionResult<ApiMessageResponse>> ChangePassword(

        [FromBody] ChangePasswordRequest request,

        CancellationToken cancellationToken)

    {

        try

        {

            var userId = GetCurrentUserId();

            var message = await _authService.ChangePasswordAsync(userId, request, cancellationToken);

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


