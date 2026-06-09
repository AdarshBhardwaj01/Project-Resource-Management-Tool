using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;

namespace PRM.DataAccess.Repositories;

public class MilestoneRepository : GenericRepository<Milestone>, IMilestoneRepository
{
    public MilestoneRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<Milestone?> GetByIdAndProjectIdAsync(
        int milestoneId,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            milestone => milestone.Id == milestoneId && milestone.ProjectId == projectId,
            cancellationToken);
    }

    public async Task<int> GetMaxSortOrderAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var maxSortOrder = await DbSet
            .Where(milestone => milestone.ProjectId == projectId)
            .Select(milestone => (int?)milestone.SortOrder)
            .MaxAsync(cancellationToken);

        return maxSortOrder ?? 0;
    }
}
