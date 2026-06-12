using PRM.Models.DTOs.Users;

namespace PRM.Business.Interfaces.Services;

public interface IUserService
{
    Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserListResponse> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDetailDto> GetUserAsync(string usernameOrId, CancellationToken cancellationToken = default);
    Task<string> ResetPasswordAsync(string usernameOrId, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
    Task<string> DeactivateUserAsync(string usernameOrId, int currentUserId, CancellationToken cancellationToken = default);
    Task<string> ReactivateUserAsync(int userId, CancellationToken cancellationToken = default);
}
