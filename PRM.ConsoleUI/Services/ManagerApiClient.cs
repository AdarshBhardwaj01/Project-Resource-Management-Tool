using System.Net.Http.Json;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.Services;

public class ManagerApiClient : ApiClientBase
{
    public ManagerApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<ResourceDashboardResponse> GetResourceDashboardAsync()
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync("api/manager/resource-dashboard");
        await EnsureSuccessAsync(response, "Failed to load resource dashboard.");
        return await ReadJsonAsync<ResourceDashboardResponse>(response)
            ?? new ResourceDashboardResponse();
    }

    public async Task<IReadOnlyList<ManagerProjectItemDto>> GetMyProjectsAsync()
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync("api/manager/projects");
        await EnsureSuccessAsync(response, "Failed to load projects.");
        return await ReadJsonAsync<List<ManagerProjectItemDto>>(response)
            ?? new List<ManagerProjectItemDto>();
    }

    public async Task<ManagerProjectDetailDto> GetMyProjectDetailAsync(int projectId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/manager/projects/{projectId}");
        await EnsureSuccessAsync(response, "Project not found.");
        return await ReadJsonAsync<ManagerProjectDetailDto>(response)
            ?? throw new InvalidOperationException("Project not found.");
    }

    public async Task<string> AllocateResourceAsync(CreateAllocationRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync("api/manager/allocations", request);
        await EnsureSuccessAsync(response, "Allocate resource failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Resource allocated.";
    }

    public async Task<EmployeeDrillDownDto> GetEmployeeDrillDownAsync(int employeeId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/manager/employees/{employeeId}");
        await EnsureSuccessAsync(response, "Employee not found.");
        return await ReadJsonAsync<EmployeeDrillDownDto>(response)
            ?? throw new InvalidOperationException("Employee not found.");
    }

    public async Task<EmployeeUtilisationPreviewDto> GetEmployeeUtilisationPreviewAsync(int employeeId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/manager/employees/{employeeId}/utilisation-preview");
        await EnsureSuccessAsync(response, "Employee not found.");
        return await ReadJsonAsync<EmployeeUtilisationPreviewDto>(response)
            ?? throw new InvalidOperationException("Employee not found.");
    }

    public async Task<AllocationValidationDto> ValidateAllocationAsync(CreateAllocationRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync("api/manager/allocations/validate", request);
        await EnsureSuccessAsync(response, "Validation failed.");
        return await ReadJsonAsync<AllocationValidationDto>(response)
            ?? throw new InvalidOperationException("Validation failed.");
    }

    public async Task<IReadOnlyList<ProjectAllocationListItemDto>> GetProjectActiveAllocationsAsync(int projectId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/manager/projects/{projectId}/allocations");
        await EnsureSuccessAsync(response, "Failed to load project allocations.");
        return await ReadJsonAsync<List<ProjectAllocationListItemDto>>(response)
            ?? new List<ProjectAllocationListItemDto>();
    }

    public async Task<string> EndAllocationAsync(int allocationId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PutAsync($"api/manager/allocations/{allocationId}/end", null);
        await EnsureSuccessAsync(response, "End allocation failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Allocation ended.";
    }

    public async Task<ManagerTeamTimesheetsResponse> GetTeamTimesheetsAsync(DateTime? weekStartDate = null)
    {
        ApplyAuthorizationHeader();
        var url = weekStartDate.HasValue
            ? $"api/manager/timesheets?weekStart={Uri.EscapeDataString(weekStartDate.Value.ToString("dd-MMM-yyyy"))}"
            : "api/manager/timesheets";
        var response = await HttpClient.GetAsync(url);
        await EnsureSuccessAsync(response, "Failed to load team timesheets.");
        return await ReadJsonAsync<ManagerTeamTimesheetsResponse>(response)
            ?? new ManagerTeamTimesheetsResponse();
    }

    public async Task<ManagerEmployeeTimesheetDetailDto> GetEmployeeTimesheetDetailAsync(
        int employeeId,
        DateTime? weekStartDate = null)
    {
        ApplyAuthorizationHeader();
        var url = weekStartDate.HasValue
            ? $"api/manager/timesheets/employees/{employeeId}?weekStart={Uri.EscapeDataString(weekStartDate.Value.ToString("dd-MMM-yyyy"))}"
            : $"api/manager/timesheets/employees/{employeeId}";
        var response = await HttpClient.GetAsync(url);
        await EnsureSuccessAsync(response, "Employee timesheet not found.");
        return await ReadJsonAsync<ManagerEmployeeTimesheetDetailDto>(response)
            ?? throw new InvalidOperationException("Employee timesheet not found.");
    }

    public async Task<SkillMatchResponse> GetSkillMatchAsync(SkillMatchRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync("api/manager/ai/skill-match", request);
        await EnsureSuccessAsync(response, "Skill match failed.");
        return await ReadJsonAsync<SkillMatchResponse>(response)
            ?? new SkillMatchResponse();
    }

    public async Task<ProjectRiskSummaryResponse> GetProjectRiskSummaryAsync(int projectId)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/manager/ai/projects/{projectId}/risk-summary");
        await EnsureSuccessAsync(response, "Risk summary not found.");
        return await ReadJsonAsync<ProjectRiskSummaryResponse>(response)
            ?? throw new InvalidOperationException("Risk summary not found.");
    }

    public async Task<TeamBuildResponse> GetTeamBuildAsync(TeamBuildRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync("api/manager/ai/team-build", request);
        await EnsureSuccessAsync(response, "Team build failed.");
        return await ReadJsonAsync<TeamBuildResponse>(response) ?? new TeamBuildResponse();
    }
}
