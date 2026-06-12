namespace PRM.Models.DTOs.EmployeePortal;

public class TimesheetSubmitPreviewResponse
{
    public string EmployeeName { get; set; } = string.Empty;

    public string WeekStartDate { get; set; } = string.Empty;

    public int MaxWeeklyHours { get; set; }

    public bool AlreadySubmitted { get; set; }

    public bool CanSubmit { get; set; }

    public IReadOnlyList<TimesheetSubmitProjectItemDto> Projects { get; set; } = [];
}
