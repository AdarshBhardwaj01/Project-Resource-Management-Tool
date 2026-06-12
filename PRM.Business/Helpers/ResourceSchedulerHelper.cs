using PRM.Common.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Helpers;

public static class ResourceSchedulerHelper
{
    public static int ComputeUtilisationPercent(Resource resource, DateTime today, int? excludeAllocationId = null)
    {
        var allocationData = resource.Allocations
            .Where(allocation => !excludeAllocationId.HasValue || allocation.Id != excludeAllocationId.Value)
            .Select(allocation => (
                allocation.FromDate,
                allocation.ToDate,
                allocation.UtilisationPercent,
                allocation.ProjectId))
            .ToList();
        return AllocationDateRules.GetDisplayUtilisationPercent(allocationData, today);
    }

    public static ResourceStatus ComputeStatus(
        Resource resource,
        DateTime today,
        IReadOnlySet<int> managerUserIds,
        int? excludeAllocationId = null)
    {
        var hasScheduledAllocation = resource.Allocations
            .Where(allocation => !excludeAllocationId.HasValue || allocation.Id != excludeAllocationId.Value)
            .Any(allocation => allocation.ToDate.Date > today);
        var managesProjects = managerUserIds.Contains(resource.UserId);
        return hasScheduledAllocation || managesProjects
            ? ResourceStatus.Allocated
            : ResourceStatus.Bench;
    }

    public static void ApplySchedulerState(
        Resource resource,
        DateTime today,
        IReadOnlySet<int> managerUserIds,
        int? excludeAllocationId = null)
    {
        resource.UtilisationPercent = ComputeUtilisationPercent(resource, today, excludeAllocationId);
        resource.Status = ComputeStatus(resource, today, managerUserIds, excludeAllocationId);
    }

    public static bool IsPartiallyAllocated(int utilisationPercent) =>
        utilisationPercent is > 0 and < 100;

    public static bool IsFullyAllocated(int utilisationPercent) =>
        utilisationPercent == 100;

    public static bool IsOverUtilised(int utilisationPercent) =>
        utilisationPercent > 100;

    public static bool IsOnBench(int utilisationPercent, bool hasScheduledAllocation) =>
        !hasScheduledAllocation || utilisationPercent == 0;
}
