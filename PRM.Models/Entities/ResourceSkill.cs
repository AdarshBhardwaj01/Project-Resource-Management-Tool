using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class ResourceSkill
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SkillId { get; set; }

    public SkillCategory Category { get; set; }

    public ProficiencyLevel ProficiencyLevel { get; set; }

    public Resource Resource { get; set; } = null!;

    public Skill Skill { get; set; } = null!;
}
