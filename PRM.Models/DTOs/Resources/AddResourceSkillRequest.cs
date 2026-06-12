namespace PRM.Models.DTOs.Resources;

public class AddResourceSkillRequest
{
    public string SkillName { get; set; } = string.Empty;

    public int Category { get; set; }

    public int ProficiencyLevel { get; set; }
}
