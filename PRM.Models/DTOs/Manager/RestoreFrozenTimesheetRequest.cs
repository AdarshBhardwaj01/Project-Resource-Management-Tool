namespace PRM.Models.DTOs.Manager;

public class RestoreFrozenTimesheetRequest
{
    public int EmployeeId { get; set; }
    public string WeekStartDate { get; set; } = string.Empty;
}
