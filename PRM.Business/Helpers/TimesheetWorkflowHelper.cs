using PRM.Common.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Helpers;

public static class TimesheetWorkflowHelper
{
    public static string GetDisplayStatus(Timesheet? timesheet, DateTime weekStartDate, DateTime today)
    {
        if (timesheet?.Status == TimesheetStatus.Submitted)
        {
            return "SUBMITTED";
        }
        if (IsFrozen(timesheet))
        {
            return "FROZEN";
        }
        if (today.Date >= WeekHelper.GetMissedEffectiveDate(weekStartDate))
        {
            return "MISSED";
        }
        return "PENDING";
    }

    public static bool IsFrozen(Timesheet? timesheet)
    {
        return timesheet is { IsFrozen: true, IsUnlockedByManager: false }
            || timesheet?.Status == TimesheetStatus.Frozen && timesheet is not { IsUnlockedByManager: true };
    }

    public static bool CanEmployeeSubmit(Timesheet? timesheet, DateTime weekStartDate, DateTime today)
    {
        if (timesheet?.Status == TimesheetStatus.Submitted)
        {
            return false;
        }
        if (IsFrozen(timesheet))
        {
            return false;
        }
        if (timesheet is { IsUnlockedByManager: true })
        {
            return true;
        }
        if (today.Date >= WeekHelper.GetMissedEffectiveDate(weekStartDate))
        {
            return false;
        }
        return today.Date <= WeekHelper.GetWeekWorkingEndDate(weekStartDate)
            || today.Date < WeekHelper.GetMissedEffectiveDate(weekStartDate);
    }

    public static int GetWorkingDaysAfterDeadline(DateTime weekStartDate, DateTime today)
    {
        var firstWorkingDayAfterDeadline = WeekHelper.GetWeekWorkingEndDate(weekStartDate).AddDays(1);
        while (!WeekHelper.IsWorkingDay(firstWorkingDayAfterDeadline)
            && firstWorkingDayAfterDeadline.Date <= today.Date)
        {
            firstWorkingDayAfterDeadline = firstWorkingDayAfterDeadline.AddDays(1);
        }
        if (today.Date < firstWorkingDayAfterDeadline.Date)
        {
            return 0;
        }
        return WeekHelper.CountWorkingDaysInclusive(firstWorkingDayAfterDeadline, today);
    }
}
