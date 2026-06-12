using AutoMapper;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Users;
using PRM.Models.Entities;
using PRM.Models.Enums;
using PRM.Common.Constants;

namespace PRM.Business.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IResourceRepository resourceRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _resourceRepository = resourceRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateUserRequest(request);
        if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
        {
            throw new BusinessValidationException("Username already exists.");
        }
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessValidationException("Email already exists.");
        }
        var role = await _roleRepository.GetByNameAsync(((ApplicationRole)request.Role).ToString(), cancellationToken)
            ?? throw new BusinessValidationException("Invalid role selected.");
        var user = _mapper.Map<User>(request);
        user.PasswordHash = _passwordHasher.Hash(request.TemporaryPassword);
        user.ForcePasswordChange = true;
        user.CreatedAt = DateTime.UtcNow;
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _userRepository.AssignRoleAsync(user.Id, role.Id, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        var appRole = (ApplicationRole)request.Role;
        if (appRole == ApplicationRole.Employee || appRole == ApplicationRole.Manager)
        {
            await _resourceRepository.AddAsync(new Resource
            {
                UserId = user.Id,
                Status = ResourceStatus.Bench,
                UtilisationPercent = 0
            }, cancellationToken);
            await _resourceRepository.SaveChangesAsync(cancellationToken);
        }
        return "Account created. User must change password on first login.";
    }

    public async Task<UserListResponse> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var userDtos = _mapper.Map<List<UserListItemDto>>(users);
        return new UserListResponse
        {
            Users = userDtos,
            Total = userDtos.Count,
            ActiveCount = userDtos.Count(user => user.Status == "Active"),
            InactiveCount = userDtos.Count(user => user.Status == "Inactive")
        };
    }

    public async Task<UserDetailDto> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(usernameOrId, cancellationToken);
        return _mapper.Map<UserDetailDto>(user);
    }

    public async Task<string> ResetPasswordAsync(
        string usernameOrId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NewTemporaryPassword))
        {
            throw new BusinessValidationException("New temporary password is required.");
        }
        PasswordValidator.Validate(request.NewTemporaryPassword);
        var user = await FindUserOrThrowAsync(usernameOrId, cancellationToken);
        user.PasswordHash = _passwordHasher.Hash(request.NewTemporaryPassword);
        user.ForcePasswordChange = true;
        await _userRepository.SaveChangesAsync(cancellationToken);
        return "Password reset. User will be prompted to change it on next login.";
    }

    public async Task<string> DeactivateUserAsync(
        string usernameOrId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(usernameOrId, cancellationToken);
        if (user.Id == currentUserId)
        {
            throw new BusinessValidationException("You cannot deactivate your own account.");
        }
        if (!user.IsActive)
        {
            throw new BusinessValidationException("User is already inactive.");
        }
        user.IsActive = false;
        await _resourceRepository.DeactivateByUserIdAsync(user.Id, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return "User deactivated.";
    }

    public async Task<string> ReactivateUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new BusinessValidationException("User not found.");
        }
        if (user.IsActive)
        {
            throw new BusinessValidationException("User is already active.");
        }
        user.IsActive = true;
        var resourceRestored = await _resourceRepository.ReactivateByUserIdAsync(userId, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return resourceRestored
            ? $"Account reactivated. {user.FullName} can now log in. Resource profile restored on BENCH."
            : $"Account reactivated. {user.FullName} can now log in.";
    }

    private async Task<User> FindUserOrThrowAsync(string usernameOrId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByUsernameOrIdAsync(usernameOrId, cancellationToken);
        if (user is null)
        {
            throw new BusinessValidationException("User not found.");
        }
        return user;
    }

    private static void ValidateCreateUserRequest(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.TemporaryPassword))
        {
            throw new BusinessValidationException("All fields are mandatory.");
        }
        if (!Enum.IsDefined(typeof(ApplicationRole), request.Role))
        {
            throw new BusinessValidationException("Invalid role selected.");
        }
        EmailValidator.Validate(request.Email);
        PasswordValidator.Validate(request.TemporaryPassword);
    }
}
