namespace PRM.Models.DTOs.Manager;

public class ManagerEmployeeTimesheetDetailDto
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string WeekStartDate { get; set; } = string.Empty;

    public int TotalHours { get; set; }

    public string Status { get; set; } = string.Empty;

    public IReadOnlyList<ManagerEmployeeTimesheetEntryDto> Entries { get; set; } = [];
}
