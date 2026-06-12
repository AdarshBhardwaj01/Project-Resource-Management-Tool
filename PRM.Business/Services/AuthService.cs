using AutoMapper;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Auth;

namespace PRM.Business.Services;

public class AuthService : IAuthService
{

    private readonly IUserRepository _userRepository;

    private readonly IPasswordHasher _passwordHasher;

    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("Invalid username or password.");
        }
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new BusinessValidationException("Invalid username or password.");
        }
        return _mapper.Map<LoginResponse>(user);
    }

    public async Task<string> ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateChangePasswordRequest(request);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("User account not found.");
        }
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.ForcePasswordChange = false;
        await _userRepository.SaveChangesAsync(cancellationToken);
        return "Password updated. Welcome!";
    }

    private static void ValidateLoginRequest(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BusinessValidationException("Username and password are required.");
        }

    }

    private static void ValidateChangePasswordRequest(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NewPassword)
            || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            throw new BusinessValidationException("New password and confirmation are required.");
        }
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new BusinessValidationException("Passwords do not match.");
        }
        PasswordValidator.Validate(request.NewPassword);
    }

}
