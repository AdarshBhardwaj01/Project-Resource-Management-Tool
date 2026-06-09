namespace PRM.Models.DTOs.EmployeePortal;

public class EmployeeTimesheetDetailDto
{
    public string WeekStartDate { get; set; } = string.Empty;

    public int TotalHours { get; set; }

    public string Status { get; set; } = string.Empty;

    public IReadOnlyList<EmployeeTimesheetEntryDetailDto> Entries { get; set; } = [];
}
