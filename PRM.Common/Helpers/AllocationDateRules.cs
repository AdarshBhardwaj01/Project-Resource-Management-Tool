namespace PRM.Common.Helpers;

public static class AllocationDateRules
{
    public static DateTime Today => DateTime.UtcNow.Date;

    public static bool IsCurrentlyActive(DateTime fromDate, DateTime toDate, DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? Today).Date;
        return fromDate.Date <= today && toDate.Date > today;
    }

    public static bool HasStarted(DateTime fromDate, DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? Today).Date;
        return fromDate.Date <= today;
    }

    public static bool HasNotExpired(DateTime toDate, DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? Today).Date;
        return toDate.Date >= today;
    }

    public static bool IsScheduled(DateTime toDate, DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? Today).Date;
        return toDate.Date > today;
    }

    public static int GetDisplayUtilisationPercent(
        IEnumerable<(DateTime FromDate, DateTime ToDate, int UtilisationPercent)> allocations,
        DateTime? referenceDate = null)
    {
        var allocationData = allocations
            .Select(allocation => (allocation.FromDate, allocation.ToDate, allocation.UtilisationPercent, ProjectId: 0))
            .ToList();
        return GetDisplayUtilisationPercent(allocationData, referenceDate);
    }

    public static int GetDisplayUtilisationPercent(
        IEnumerable<(DateTime FromDate, DateTime ToDate, int UtilisationPercent, int ProjectId)> allocations,
        DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? Today).Date;
        var scheduled = allocations.Where(allocation => allocation.ToDate.Date > today).ToList();
        if (scheduled.Count == 0)
        {
            return 0;
        }
        var currentlyActivePercent = scheduled
            .Where(allocation => IsCurrentlyActive(allocation.FromDate, allocation.ToDate, today))
            .Sum(allocation => allocation.UtilisationPercent);
        var maxProjectScheduledPercent = scheduled
            .GroupBy(allocation => allocation.ProjectId)
            .Max(group => group.Sum(allocation => allocation.UtilisationPercent));
        if (currentlyActivePercent == 0)
        {
            var futureScheduledPercent = scheduled
                .Where(allocation => allocation.FromDate.Date > today)
                .Sum(allocation => allocation.UtilisationPercent);
            return Math.Max(futureScheduledPercent, maxProjectScheduledPercent);
        }
        return Math.Max(currentlyActivePercent, maxProjectScheduledPercent);
    }

    public static bool HasScheduledAllocation(
        IEnumerable<(DateTime FromDate, DateTime ToDate, int UtilisationPercent)> allocations,
        DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? Today).Date;
        return allocations.Any(allocation => allocation.ToDate.Date > today);
    }

    public static bool OverlapsPeriod(
        DateTime fromDate,
        DateTime toDate,
        DateTime periodStart,
        DateTime periodEnd)
    {
        return fromDate.Date <= periodEnd.Date && toDate.Date >= periodStart.Date;
    }

    public static bool CountsTowardPeriodUtilisation(
        DateTime fromDate,
        DateTime toDate,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime? referenceDate = null)
    {
        if (!OverlapsPeriod(fromDate, toDate, periodStart, periodEnd))
        {
            return false;
        }
        var today = (referenceDate ?? Today).Date;
        if (periodStart.Date > today)
        {
            return fromDate.Date <= today && toDate.Date > today;
        }
        return fromDate.Date <= periodStart.Date && toDate.Date > periodStart.Date;
    }
}
