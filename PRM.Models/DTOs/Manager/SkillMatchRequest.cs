namespace PRM.Models.DTOs.Manager;

public class SkillMatchRequest
{
    public string Requirement { get; set; } = string.Empty;

    public int? ProjectId { get; set; }
}
