namespace PRM.Models.DTOs.Resources;

public class ResourceSkillDto
{
    public int SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ProficiencyLevel { get; set; } = string.Empty;
}
