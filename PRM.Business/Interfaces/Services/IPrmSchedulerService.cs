namespace PRM.Business.Interfaces.Services;

public interface IPrmSchedulerService
{
    Task RunScheduledTasksAsync(CancellationToken cancellationToken = default);

    Task RecomputeEmployeeAsync(
        int employeeId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);

    Task RecomputeAllEmployeesAsync(CancellationToken cancellationToken = default);

    Task RecomputeProjectHealthAsync(CancellationToken cancellationToken = default);

    Task RecomputeProjectHealthAsync(
        int projectId,
        CancellationToken cancellationToken = default);
}
