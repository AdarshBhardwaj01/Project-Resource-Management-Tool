using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class Skill
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<ResourceSkill> ResourceSkills { get; set; } = new List<ResourceSkill>();
}
