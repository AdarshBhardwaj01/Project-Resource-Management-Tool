using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class PrmSchedulerService : IPrmSchedulerService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISystemConfigRepository _systemConfigRepository;

    public PrmSchedulerService(
        IEmployeeRepository employeeRepository,
        IProjectRepository projectRepository,
        ISystemConfigRepository systemConfigRepository)
    {
        _employeeRepository = employeeRepository;
        _projectRepository = projectRepository;
        _systemConfigRepository = systemConfigRepository;
    }

    public async Task RunScheduledTasksAsync(CancellationToken cancellationToken = default)
    {
        await RecomputeAllEmployeesInternalAsync(cancellationToken);
        await RecomputeProjectHealthAsync(cancellationToken);
    }

    public async Task RecomputeEmployeeAsync(
        int employeeId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdForSchedulerUpdateAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return;
        }

        var managerUserIds = await GetManagerUserIdSetAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        EmployeeSchedulerHelper.ApplySchedulerState(
            employee,
            today,
            managerUserIds,
            excludeAllocationId);

        await _employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public Task RecomputeAllEmployeesAsync(CancellationToken cancellationToken = default) =>
        RecomputeAllEmployeesInternalAsync(cancellationToken);

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

    private async Task RecomputeAllEmployeesInternalAsync(CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetAllActiveWithAllocationsAsync(cancellationToken);
        var managerUserIds = await GetManagerUserIdSetAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var hasChanges = false;

        foreach (var employee in employees)
        {
            var previousStatus = employee.Status;
            var previousUtilisation = employee.UtilisationPercent;

            EmployeeSchedulerHelper.ApplySchedulerState(employee, today, managerUserIds);

            if (employee.Status != previousStatus || employee.UtilisationPercent != previousUtilisation)
            {
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _employeeRepository.SaveChangesAsync(cancellationToken);
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
                allocation.Employee.IsActive &&
                allocation.Employee.User.Role == UserRole.Employee)
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
