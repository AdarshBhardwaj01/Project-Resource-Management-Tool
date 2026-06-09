namespace PRM.Models.DTOs.Manager;

public class ManagerTeamTimesheetsResponse
{
    public string WeekStartDate { get; set; } = string.Empty;

    public IReadOnlyList<ManagerTeamTimesheetRowDto> Rows { get; set; } = [];
}
