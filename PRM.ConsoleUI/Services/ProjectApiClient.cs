using System.Net.Http.Json;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.Projects;

namespace PRM.ConsoleUI.Services;

public class ProjectApiClient : ApiClientBase
{
    public ProjectApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<string> CreateProjectAsync(CreateProjectRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync("api/projects", request);
        await EnsureSuccessAsync(response, "Create project failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Project created.";
    }

    public async Task<ProjectListResponse> GetAllProjectsAsync(string? status = null)
    {
        ApplyAuthorizationHeader();
        var url = string.IsNullOrWhiteSpace(status)
            ? "api/projects"
            : $"api/projects?status={Uri.EscapeDataString(status)}";
        var response = await HttpClient.GetAsync(url);
        await EnsureSuccessAsync(response, "Failed to load projects.");
        return await ReadJsonAsync<ProjectListResponse>(response)
            ?? new ProjectListResponse();
    }

    public async Task<ProjectDetailDto> GetProjectAsync(int id)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync($"api/projects/{id}");
        await EnsureSuccessAsync(response, "Project not found.");
        return await ReadJsonAsync<ProjectDetailDto>(response)
            ?? throw new InvalidOperationException("Project not found.");
    }

    public async Task<string> UpdateProjectAsync(int id, UpdateProjectRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PutAsJsonAsync($"api/projects/{id}", request);
        await EnsureSuccessAsync(response, "Update project failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Project updated.";
    }

    public async Task<string> AddMilestoneAsync(int projectId, CreateMilestoneRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PostAsJsonAsync($"api/projects/{projectId}/milestones", request);
        await EnsureSuccessAsync(response, "Add milestone failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Milestone added.";
    }

    public async Task<string> UpdateMilestoneAsync(int projectId, int milestoneId, UpdateMilestoneRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PutAsJsonAsync($"api/projects/{projectId}/milestones/{milestoneId}", request);
        await EnsureSuccessAsync(response, "Update milestone failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Milestone updated.";
    }
}
