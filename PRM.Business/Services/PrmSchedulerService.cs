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

    public PrmSchedulerService(
        IResourceRepository resourceRepository,
        IProjectRepository projectRepository,
        ISystemConfigRepository systemConfigRepository)
    {
        _resourceRepository = resourceRepository;
        _projectRepository = projectRepository;
        _systemConfigRepository = systemConfigRepository;
    }

    public async Task RunScheduledTasksAsync(CancellationToken cancellationToken = default)
    {
        await RecomputeAllResourcesInternalAsync(cancellationToken);
        await RecomputeProjectHealthAsync(cancellationToken);
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
            if (ApplyProjectHealth(project, today, maxWeeklyHours))
            {
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
        if (ApplyProjectHealth(project, today, maxWeeklyHours))
        {
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
        var allocations = project.Allocations
            .Where(allocation =>
                allocation.ToDate.Date > today &&
                allocation.Resource.User.IsActive &&
                UserRoleHelper.HasRole(allocation.Resource.User, ApplicationRole.Employee))
            .ToList();
        var healthStatus = ProjectHealthCalculator.Compute(
            project,
            allocations,
            today,
            maxWeeklyHours);
        if (project.HealthStatus == healthStatus)
        {
            return false;
        }
        project.HealthStatus = healthStatus;
        return true;
    }
}
