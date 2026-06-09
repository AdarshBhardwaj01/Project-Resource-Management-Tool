namespace PRM.Business.Interfaces.Services;

public interface IEmployeeStatusSchedulerService
{
    Task RecomputeEmployeeStatusAsync(
        int employeeId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);

    Task RecomputeEmployeeStatusByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
