using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Interfaces.Repositories;

public interface IResourceRepository : IRepository<Resource>
{
    Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> RestoreInactiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ReactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Resource?> GetByUserIdWithDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Resource?> GetByUserIdForSchedulerUpdateAsync(int userId, CancellationToken cancellationToken = default);
    Task<Resource?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Resource?> GetActiveResourceByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetAllAsync(
        ResourceStatus? status,
        string? department,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetResourcesWithSkillsForDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetAllActiveResourcesWithSkillsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetTeamResourcesWithAllocationsAsync(
        int managerUserId,
        DateTime weekStart,
        DateTime weekEnd,
        CancellationToken cancellationToken = default);
    Task<Resource?> GetResourceForDrillDownAsync(
        int userId,
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetAllActiveWithAllocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetActiveResourceUserIdsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAssignedToManagerAsync(
        int userId,
        int managerUserId,
        CancellationToken cancellationToken = default);
}
