namespace PRM.Models.DTOs.Manager;

public class ManagerEmployeeTimesheetEntryDto
{
    public string ProjectName { get; set; } = string.Empty;

    public int Hours { get; set; }

    public string ActivityTags { get; set; } = string.Empty;
}
