namespace PRM.Models.DTOs.Ai;

public class AiSkillMatchCandidateDto
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public bool IsOnBench { get; set; }

    public string Availability { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string MatchedSkills { get; set; } = string.Empty;

    public string RecentActivity { get; set; } = string.Empty;
}
