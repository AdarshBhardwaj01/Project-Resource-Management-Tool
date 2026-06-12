namespace PRM.Models.DTOs.Manager;

public class FrozenTimesheetItemDto
{
    public int RowNumber { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string WeekStartDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReminderCount { get; set; }
}
