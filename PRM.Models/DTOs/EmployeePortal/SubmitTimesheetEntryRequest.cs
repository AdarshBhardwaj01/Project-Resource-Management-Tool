namespace PRM.Models.DTOs.EmployeePortal;

public class SubmitTimesheetEntryRequest
{
    public int ProjectId { get; set; }

    public decimal Hours { get; set; }

    public string ActivityTags { get; set; } = string.Empty;
}
