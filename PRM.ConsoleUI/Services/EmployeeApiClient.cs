using System.Net.Http.Json;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Employees;

namespace PRM.ConsoleUI.Services;

public class EmployeeApiClient : ApiClientBase
{
    public EmployeeApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<string> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PostAsJsonAsync("api/employees", request);
        await EnsureSuccessAsync(response, "Create employee failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Employee created.";
    }

    public async Task<EmployeeListResponse> GetAllEmployeesAsync(string? status = null, string? department = null)
    {
        ApplyAuthorizationHeader();

        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query.Add($"department={Uri.EscapeDataString(department)}");
        }

        var url = query.Count > 0
            ? $"api/employees?{string.Join("&", query)}"
            : "api/employees";

        var response = await HttpClient.GetAsync(url);
        await EnsureSuccessAsync(response, "Failed to load employees.");

        return await ReadJsonAsync<EmployeeListResponse>(response)
            ?? new EmployeeListResponse();
    }

    public async Task<EmployeeDetailDto> GetEmployeeAsync(int id)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.GetAsync($"api/employees/{id}");
        await EnsureSuccessAsync(response, "Employee not found.");

        return await ReadJsonAsync<EmployeeDetailDto>(response)
            ?? throw new InvalidOperationException("Employee not found.");
    }

    public async Task<string> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PutAsJsonAsync($"api/employees/{id}", request);
        await EnsureSuccessAsync(response, "Update employee failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Employee updated.";
    }

    public async Task<string> DeactivateEmployeeAsync(int id)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PutAsync($"api/employees/{id}/deactivate", null);
        await EnsureSuccessAsync(response, "Deactivate employee failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Employee deactivated.";
    }

    public async Task<IReadOnlyList<EmployeeSkillDto>> GetEmployeeSkillsAsync(int employeeId)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.GetAsync($"api/employees/{employeeId}/skills");
        await EnsureSuccessAsync(response, "Failed to load skills.");

        return await ReadJsonAsync<List<EmployeeSkillDto>>(response)
            ?? new List<EmployeeSkillDto>();
    }

    public async Task<string> AddEmployeeSkillAsync(int employeeId, AddEmployeeSkillRequest request)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PostAsJsonAsync($"api/employees/{employeeId}/skills", request);
        await EnsureSuccessAsync(response, "Add skill failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Skill added.";
    }

    public async Task<string> UpdateEmployeeSkillAsync(int employeeId, int skillId, UpdateEmployeeSkillRequest request)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PutAsJsonAsync($"api/employees/{employeeId}/skills/{skillId}", request);
        await EnsureSuccessAsync(response, "Update skill failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Proficiency level updated.";
    }

    public async Task<string> RemoveEmployeeSkillAsync(int employeeId, int skillId)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.DeleteAsync($"api/employees/{employeeId}/skills/{skillId}");
        await EnsureSuccessAsync(response, "Remove skill failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Skill removed.";
    }

    public async Task<string> AssignManagerAsync(AssignManagerRequest request)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PutAsJsonAsync("api/employees/assign-manager", request);
        await EnsureSuccessAsync(response, "Assign manager failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Manager assigned.";
    }
}
