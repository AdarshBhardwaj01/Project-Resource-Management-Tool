namespace PRM.Models.DTOs.Manager;

public class SkillMatchRequest
{
    public string Requirement { get; set; } = string.Empty;

    public int? ProjectId { get; set; }

    public bool SearchEntireOrganization { get; set; }

    public bool RequireSingleEmployeeMatch { get; set; }

    public int MaxSuggestions { get; set; } = 2;
}
