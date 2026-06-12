using PRM.Models.DTOs.Manager;

namespace PRM.Business.Interfaces.Services;

public interface IManagerService
{
    Task<ResourceDashboardResponse> GetResourceDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerProjectItemDto>> GetMyProjectsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<ManagerProjectDetailDto> GetMyProjectDetailAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default);
    Task<string> AllocateResourceAsync(
        int managerUserId,
        CreateAllocationRequest request,
        CancellationToken cancellationToken = default);
    Task<EmployeeDrillDownDto> GetEmployeeDrillDownAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default);
    Task<EmployeeUtilisationPreviewDto> GetEmployeeUtilisationPreviewAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default);
    Task<AllocationValidationDto> ValidateAllocationAsync(
        int managerUserId,
        CreateAllocationRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocationListItemDto>> GetProjectActiveAllocationsAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default);
    Task<string> EndAllocationAsync(
        int managerUserId,
        int allocationId,
        CancellationToken cancellationToken = default);
    Task<ManagerTeamTimesheetsResponse> GetTeamTimesheetsAsync(
        int managerUserId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default);
    Task<ManagerEmployeeTimesheetDetailDto> GetEmployeeTimesheetDetailAsync(
        int managerUserId,
        int employeeId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default);
    Task<SkillMatchResponse> GetSkillMatchAsync(
        int managerUserId,
        SkillMatchRequest request,
        CancellationToken cancellationToken = default);
    Task<ProjectRiskSummaryResponse> GetProjectRiskSummaryAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default);
    Task<TeamBuildResponse> BuildTeamAsync(
        int managerUserId,
        TeamBuildRequest request,
        CancellationToken cancellationToken = default);
}
