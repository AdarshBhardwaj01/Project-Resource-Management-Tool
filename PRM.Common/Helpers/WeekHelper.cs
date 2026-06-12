namespace PRM.Common.Helpers;

public static class WeekHelper
{
    public static DateTime GetWeekStartDate(DateTime date)
    {
        var day = date.Date;
        var offset = ((int)day.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return day.AddDays(-offset);
    }

    public static DateTime GetCurrentWeekStartDate()
    {
        return GetWeekStartDate(DateTime.UtcNow.Date);
    }

    public static DateTime GetWeekEndDate(DateTime weekStartDate)
    {
        return weekStartDate.Date.AddDays(6);
    }

    public static DateTime GetWeekWorkingEndDate(DateTime weekStartDate)
    {
        return weekStartDate.Date.AddDays(4);
    }

    public static DateTime GetMissedEffectiveDate(DateTime weekStartDate)
    {
        return weekStartDate.Date.AddDays(6);
    }

    public static bool IsWorkingDay(DateTime date)
    {
        return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
    }

    public static int CountWorkingDaysInclusive(DateTime fromDate, DateTime toDate)
    {
        if (toDate.Date < fromDate.Date)
        {
            return 0;
        }
        var count = 0;
        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
        {
            if (IsWorkingDay(date))
            {
                count++;
            }
        }
        return count;
    }
}
