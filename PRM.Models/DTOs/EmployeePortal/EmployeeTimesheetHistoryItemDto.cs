namespace PRM.Models.DTOs.EmployeePortal;

public class EmployeeTimesheetHistoryItemDto
{
    public int TimesheetId { get; set; }

    public string WeekStartDate { get; set; } = string.Empty;

    public int TotalHours { get; set; }

    public string Status { get; set; } = string.Empty;
}
