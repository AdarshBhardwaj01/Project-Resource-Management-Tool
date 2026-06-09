namespace PRM.Models.DTOs.EmployeePortal;

public class TimesheetSubmitProjectItemDto
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public int ExpectedMaxHours { get; set; }
}
