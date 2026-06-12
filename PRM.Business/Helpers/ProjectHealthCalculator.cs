using PRM.Common.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Helpers;

public static class ProjectHealthCalculator
{
    public static ProjectHealthStatus ComputeForProject(
        Project project,
        DateTime today,
        int maxWeeklyHours)
    {
        return Compute(
            project,
            GetActiveEmployeeAllocations(project, today),
            today,
            maxWeeklyHours);
    }

    public static IReadOnlyList<Allocation> GetActiveEmployeeAllocations(Project project, DateTime today)
    {
        return project.Allocations
            .Where(allocation =>
                allocation.ToDate.Date > today &&
                allocation.Resource.User.IsActive &&
                UserRoleHelper.HasRole(allocation.Resource.User, ApplicationRole.Employee))
            .ToList();
    }

    public static ProjectHealthStatus Compute(
        Project project,
        IReadOnlyList<Allocation> allocations,
        DateTime today,
        int maxWeeklyHours)
    {
        if (project.Milestones.Any(milestone =>
                milestone.DueDate.Date < today &&
                milestone.Status != MilestoneStatus.Done))
        {
            return ProjectHealthStatus.AtRisk;
        }
        var lastWeekStart = today.AddDays(-7);
        foreach (var allocation in allocations.Where(allocation =>
                     AllocationDateRules.IsCurrentlyActive(allocation.FromDate, allocation.ToDate, today)))
        {
            var expectedHours = allocation.UtilisationPercent * maxWeeklyHours / 100;
            var loggedHours = project.TimesheetEntries
                .Where(entry =>
                    entry.Timesheet.UserId == allocation.UserId &&
                    entry.Timesheet.WeekStartDate.Date >= lastWeekStart.AddDays(-6))
                .Sum(entry => entry.Hours);
            if (loggedHours < expectedHours)
            {
                return ProjectHealthStatus.Attention;
            }
        }
        return ProjectHealthStatus.OnTrack;
    }
}
