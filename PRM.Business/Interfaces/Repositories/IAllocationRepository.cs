using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface IAllocationRepository : IRepository<Allocation>
{
    Task<IReadOnlyList<Allocation>> GetAllAsync(
        int? employeeId,
        int? projectId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<int> GetActiveUtilisationTotalAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<int> GetOverlappingUtilisationTotalAsync(
        int employeeId,
        DateTime fromDate,
        DateTime toDate,
        int? excludeAllocationId = null,
        int? excludeProjectId = null,
        CancellationToken cancellationToken = default);

    Task<Allocation?> GetOverlappingAllocationOnProjectAsync(
        int employeeId,
        int projectId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveAllocationsAsync(
        int employeeId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);

    Task<Allocation?> GetByIdForUpdateAsync(int allocationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetByManagerIdForPeriodAsync(
        int managerUserId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetByEmployeeIdForPeriodAsync(
        int employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetScheduledByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);
}
