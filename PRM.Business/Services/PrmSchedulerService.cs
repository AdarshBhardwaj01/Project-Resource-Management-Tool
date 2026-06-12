using Microsoft.Extensions.Logging;
using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class PrmSchedulerService : IPrmSchedulerService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISystemConfigRepository _systemConfigRepository;
    private readonly ITimesheetSchedulerService _timesheetSchedulerService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<PrmSchedulerService> _logger;

    public PrmSchedulerService(
        IResourceRepository resourceRepository,
        IProjectRepository projectRepository,
        ISystemConfigRepository systemConfigRepository,
        ITimesheetSchedulerService timesheetSchedulerService,
        IEmailNotificationService emailNotificationService,
        ILogger<PrmSchedulerService> logger)
    {
        _resourceRepository = resourceRepository;
        _projectRepository = projectRepository;
        _systemConfigRepository = systemConfigRepository;
        _timesheetSchedulerService = timesheetSchedulerService;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    public async Task RunScheduledTasksAsync(CancellationToken cancellationToken = default)
    {
        await RecomputeAllResourcesInternalAsync(cancellationToken);
        await RecomputeProjectHealthAsync(cancellationToken);
        await _timesheetSchedulerService.ProcessTimesheetWorkflowAsync(cancellationToken);
    }

    public async Task RecomputeResourceAsync(
        int userId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByUserIdForSchedulerUpdateAsync(userId, cancellationToken);
        if (resource is null || !resource.User.IsActive)
        {
            return;
        }
        var managerUserIds = await GetManagerUserIdSetAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        ResourceSchedulerHelper.ApplySchedulerState(
            resource,
            today,
            managerUserIds,
            excludeAllocationId);
        await _resourceRepository.SaveChangesAsync(cancellationToken);
    }

    public Task RecomputeAllResourcesAsync(CancellationToken cancellationToken = default) =>
        RecomputeAllResourcesInternalAsync(cancellationToken);

    public async Task RecomputeProjectHealthAsync(CancellationToken cancellationToken = default)
    {
        var config = await _systemConfigRepository.GetSingletonAsync(cancellationToken);
        var maxWeeklyHours = config?.MaxWeeklyHours ?? SystemDefaults.MaxWeeklyHours;
        var today = DateTime.UtcNow.Date;
        var projects = await _projectRepository.GetAllForHealthSchedulerAsync(cancellationToken);
        var hasChanges = false;
        foreach (var project in projects)
        {
            var previousHealth = project.HealthStatus;
            if (ApplyProjectHealth(project, today, maxWeeklyHours))
            {
                hasChanges = true;
                if (project.HealthStatus == ProjectHealthStatus.AtRisk
                    && project.AtRiskNotificationSentAt is null)
                {
                    await SendProjectAtRiskNotificationAsync(project, cancellationToken);
                    project.AtRiskNotificationSentAt = DateTime.UtcNow;
                }
                else if (project.HealthStatus != ProjectHealthStatus.AtRisk)
                {
                    project.AtRiskNotificationSentAt = null;
                }
            }
            else if (previousHealth == ProjectHealthStatus.AtRisk
                && project.HealthStatus != ProjectHealthStatus.AtRisk)
            {
                project.AtRiskNotificationSentAt = null;
                hasChanges = true;
            }
        }
        if (hasChanges)
        {
            await _projectRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RecomputeProjectHealthAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var config = await _systemConfigRepository.GetSingletonAsync(cancellationToken);
        var maxWeeklyHours = config?.MaxWeeklyHours ?? SystemDefaults.MaxWeeklyHours;
        var today = DateTime.UtcNow.Date;
        var project = await _projectRepository.GetByIdForHealthSchedulerAsync(projectId, cancellationToken);
        if (project is null)
        {
            return;
        }
        var previousHealth = project.HealthStatus;
        if (ApplyProjectHealth(project, today, maxWeeklyHours))
        {
            if (project.HealthStatus == ProjectHealthStatus.AtRisk
                && project.AtRiskNotificationSentAt is null)
            {
                await SendProjectAtRiskNotificationAsync(project, cancellationToken);
                project.AtRiskNotificationSentAt = DateTime.UtcNow;
            }
            else if (project.HealthStatus != ProjectHealthStatus.AtRisk)
            {
                project.AtRiskNotificationSentAt = null;
            }
            await _projectRepository.SaveChangesAsync(cancellationToken);
        }
        else if (previousHealth == ProjectHealthStatus.AtRisk
            && project.HealthStatus != ProjectHealthStatus.AtRisk)
        {
            project.AtRiskNotificationSentAt = null;
            await _projectRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RecomputeAllResourcesInternalAsync(CancellationToken cancellationToken)
    {
        var resources = await _resourceRepository.GetAllActiveWithAllocationsAsync(cancellationToken);
        var managerUserIds = await GetManagerUserIdSetAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var hasChanges = false;
        foreach (var resource in resources)
        {
            var previousStatus = resource.Status;
            var previousUtilisation = resource.UtilisationPercent;
            ResourceSchedulerHelper.ApplySchedulerState(resource, today, managerUserIds);
            if (resource.Status != previousStatus || resource.UtilisationPercent != previousUtilisation)
            {
                hasChanges = true;
            }
        }
        if (hasChanges)
        {
            await _resourceRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<HashSet<int>> GetManagerUserIdSetAsync(CancellationToken cancellationToken)
    {
        var managerUserIds = await _projectRepository.GetManagerUserIdsAsync(cancellationToken);
        return managerUserIds.ToHashSet();
    }

    private static bool ApplyProjectHealth(Project project, DateTime today, int maxWeeklyHours)
    {
        var healthStatus = ProjectHealthCalculator.ComputeForProject(project, today, maxWeeklyHours);
        if (project.HealthStatus == healthStatus)
        {
            return false;
        }
        project.HealthStatus = healthStatus;
        return true;
    }

    private async Task SendProjectAtRiskNotificationAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        if (project.Manager is null)
        {
            _logger.LogWarning(
                "Project {ProjectId} is AT RISK but manager details are unavailable for email notification.",
                project.Id);
            return;
        }
        var summary = BuildAtRiskSummary(project);
        await _emailNotificationService.SendProjectAtRiskAsync(
            project.Manager.Email,
            project.Manager.FullName,
            project.Name,
            "AT RISK",
            summary,
            cancellationToken);
        _logger.LogInformation(
            "Sent AT RISK notification for project {ProjectName} to manager {ManagerEmail}.",
            project.Name,
            project.Manager.Email);
    }

    private static string BuildAtRiskSummary(Project project)
    {
        var today = DateTime.UtcNow.Date;
        var overdueMilestones = project.Milestones
            .Where(milestone => milestone.DueDate.Date < today && milestone.Status != MilestoneStatus.Done)
            .Select(milestone => milestone.Title)
            .ToList();
        if (overdueMilestones.Count > 0)
        {
            return $"Project health is AT RISK due to overdue milestone(s): {string.Join(", ", overdueMilestones)}.";
        }
        return "Project health is AT RISK. Review milestones, allocations, and recent timesheet submissions.";
    }
}
