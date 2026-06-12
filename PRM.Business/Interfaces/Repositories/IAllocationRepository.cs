using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface IAllocationRepository : IRepository<Allocation>
{
    Task<IReadOnlyList<Allocation>> GetAllAsync(
        int? userId,
        int? projectId,
        string? status,
        CancellationToken cancellationToken = default);
    Task<int> GetActiveUtilisationTotalAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetOverlappingUtilisationTotalAsync(
        int userId,
        DateTime fromDate,
        DateTime toDate,
        int? excludeAllocationId = null,
        int? excludeProjectId = null,
        CancellationToken cancellationToken = default);
    Task<Allocation?> GetOverlappingAllocationOnProjectAsync(
        int userId,
        int projectId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveAllocationsAsync(
        int userId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<Allocation?> GetByIdForUpdateAsync(int allocationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByManagerIdForPeriodAsync(
        int managerUserId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetByUserIdForPeriodAsync(
        int userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetScheduledByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
