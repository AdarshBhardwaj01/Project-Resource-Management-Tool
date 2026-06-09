namespace PRM.Models.DTOs.EmployeePortal;

public class EmployeeTimesheetEntryDetailDto
{
    public string ProjectName { get; set; } = string.Empty;

    public int Hours { get; set; }

    public string ActivityTags { get; set; } = string.Empty;
}
