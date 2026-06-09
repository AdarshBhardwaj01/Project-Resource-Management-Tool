using PRM.Common.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Helpers;

public static class EmployeeSchedulerHelper
{
    public static int ComputeUtilisationPercent(Employee employee, DateTime today, int? excludeAllocationId = null)
    {
        var allocationData = employee.Allocations
            .Where(allocation => !excludeAllocationId.HasValue || allocation.Id != excludeAllocationId.Value)
            .Select(allocation => (
                allocation.FromDate,
                allocation.ToDate,
                allocation.UtilisationPercent,
                allocation.ProjectId))
            .ToList();

        return AllocationDateRules.GetDisplayUtilisationPercent(allocationData, today);
    }

    public static EmployeeStatus ComputeStatus(
        Employee employee,
        DateTime today,
        IReadOnlySet<int> managerUserIds,
        int? excludeAllocationId = null)
    {
        var hasScheduledAllocation = employee.Allocations
            .Where(allocation => !excludeAllocationId.HasValue || allocation.Id != excludeAllocationId.Value)
            .Any(allocation => allocation.ToDate.Date > today);

        var managesProjects = managerUserIds.Contains(employee.UserId);

        return hasScheduledAllocation || managesProjects
            ? EmployeeStatus.Allocated
            : EmployeeStatus.Bench;
    }

    public static void ApplySchedulerState(
        Employee employee,
        DateTime today,
        IReadOnlySet<int> managerUserIds,
        int? excludeAllocationId = null)
    {
        employee.UtilisationPercent = ComputeUtilisationPercent(employee, today, excludeAllocationId);
        employee.Status = ComputeStatus(employee, today, managerUserIds, excludeAllocationId);
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
