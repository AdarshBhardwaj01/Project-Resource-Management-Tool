namespace PRM.Models.DTOs.Manager;

public class SkillMatchSuggestionDto
{
    public int RowNumber { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string SkillsMatch { get; set; } = string.Empty;

    public string Availability { get; set; } = string.Empty;

    public string RecentActivity { get; set; } = string.Empty;
}
