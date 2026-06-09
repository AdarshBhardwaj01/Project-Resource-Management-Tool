namespace PRM.Models.DTOs.EmployeePortal;

public class SubmitTimesheetRequest
{
    public string? WeekStartDate { get; set; }

    public IReadOnlyList<SubmitTimesheetEntryRequest> Entries { get; set; } = [];
}
