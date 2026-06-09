using PRM.Models.DTOs.Projects;

namespace PRM.Business.Interfaces.Services;

public interface IProjectService
{
    Task<string> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);

    Task<ProjectListResponse> GetAllProjectsAsync(string? status, CancellationToken cancellationToken = default);

    Task<ProjectDetailDto> GetProjectAsync(int id, CancellationToken cancellationToken = default);

    Task<string> UpdateProjectAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken = default);

    Task<string> AddMilestoneAsync(int projectId, CreateMilestoneRequest request, CancellationToken cancellationToken = default);

    Task<string> UpdateMilestoneAsync(
        int projectId,
        int milestoneId,
        UpdateMilestoneRequest request,
        CancellationToken cancellationToken = default);
}
