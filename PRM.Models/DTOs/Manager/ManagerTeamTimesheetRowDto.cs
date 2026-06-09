namespace PRM.Models.DTOs.Manager;

public class ManagerTeamTimesheetRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public int Hours { get; set; }

    public string Status { get; set; } = string.Empty;
}
