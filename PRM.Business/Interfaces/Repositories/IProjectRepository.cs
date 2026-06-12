using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Interfaces.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllAsync(ProjectStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByManagerIdAsync(int managerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByManagerIdWithDetailsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<Project?> GetByIdForManagerAsync(int projectId, int managerUserId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdForManagerWithDetailsAsync(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<bool> HasManagedProjectsAsync(int managerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllForHealthSchedulerAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetByIdForHealthSchedulerAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetManagerUserIdsAsync(CancellationToken cancellationToken = default);
}
