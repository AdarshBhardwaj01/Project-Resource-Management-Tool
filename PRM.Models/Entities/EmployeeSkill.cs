using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class EmployeeSkill
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int SkillId { get; set; }

    public SkillCategory Category { get; set; }

    public ProficiencyLevel ProficiencyLevel { get; set; }

    public Employee Employee { get; set; } = null!;

    public Skill Skill { get; set; } = null!;
}
