using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface IMilestoneRepository : IRepository<Milestone>
{
    Task<Milestone?> GetByIdAndProjectIdAsync(
        int milestoneId,
        int projectId,
        CancellationToken cancellationToken = default);

    Task<int> GetMaxSortOrderAsync(int projectId, CancellationToken cancellationToken = default);
}
