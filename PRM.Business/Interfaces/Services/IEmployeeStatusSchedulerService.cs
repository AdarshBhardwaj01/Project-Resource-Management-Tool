namespace PRM.Business.Interfaces.Services;

public interface IEmployeeStatusSchedulerService
{
    Task RecomputeResourceStatusAsync(
        int userId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);
    Task RecomputeResourceStatusByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
