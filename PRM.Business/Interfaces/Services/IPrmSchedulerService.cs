namespace PRM.Business.Interfaces.Services;

public interface IPrmSchedulerService
{
    Task RunScheduledTasksAsync(CancellationToken cancellationToken = default);
    Task RecomputeResourceAsync(
        int userId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);
    Task RecomputeAllResourcesAsync(CancellationToken cancellationToken = default);
    Task RecomputeProjectHealthAsync(CancellationToken cancellationToken = default);
    Task RecomputeProjectHealthAsync(
        int projectId,
        CancellationToken cancellationToken = default);
}
