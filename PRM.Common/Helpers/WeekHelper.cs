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
}
