namespace PRM.Models.DTOs.Manager;

public class TeamRoleResultDto
{
    public int SlotNumber { get; set; }
    public string RoleLabel { get; set; } = string.Empty;
    public bool Filled { get; set; }
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string MatchedSkills { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string GapReason { get; set; } = string.Empty;
}
