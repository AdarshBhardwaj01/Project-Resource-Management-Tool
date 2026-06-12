using System.Net.Http.Json;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Users;

namespace PRM.ConsoleUI.Services;

public class UserApiClient : ApiClientBase
{
    public UserApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<string> CreateUserAsync(CreateUserRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync("api/users", request);
        await EnsureSuccessAsync(response, "Create user failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Account created.";
    }

    public async Task<UserListResponse> GetAllUsersAsync()
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync("api/users");
        await EnsureSuccessAsync(response, "Failed to load users.");
        return await ReadJsonAsync<UserListResponse>(response)
            ?? new UserListResponse();
    }

    public async Task<UserDetailDto> GetUserAsync(string usernameOrId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/users/{Uri.EscapeDataString(usernameOrId)}");
        await EnsureSuccessAsync(response, "User not found.");
        return await ReadJsonAsync<UserDetailDto>(response)
            ?? throw new InvalidOperationException("User not found.");
    }

    public async Task<string> ResetPasswordAsync(string usernameOrId, ResetUserPasswordRequest request)
    {
        ApplyAuthorizationHeader();
        var encoded = Uri.EscapeDataString(usernameOrId);
        var response = await HttpClient.PutAsJsonAsync($"api/users/{encoded}/reset-password", request);
        await EnsureSuccessAsync(response, "Password reset failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Password reset.";
    }

    public async Task<string> DeactivateUserAsync(string usernameOrId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PutAsync($"api/users/{Uri.EscapeDataString(usernameOrId)}/deactivate", null);
        await EnsureSuccessAsync(response, "Deactivate user failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "User deactivated.";
    }

    public async Task<string> ReactivateUserAsync(int userId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PutAsync($"api/users/{userId}/reactivate", null);
        await EnsureSuccessAsync(response, "Reactivate user failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "User reactivated.";
    }
}
