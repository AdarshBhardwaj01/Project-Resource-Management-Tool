namespace PRM.Models.DTOs.Ai;

public class AiSkillMatchContext
{

    public string Requirement { get; set; } = string.Empty;

    public string? ProjectName { get; set; }

    public int? MinAvailablePercent { get; set; }

    public DateTime? AvailableFromDate { get; set; }

    public bool RequireFullAvailability { get; set; }

    public bool RequireSingleEmployeeMatch { get; set; }

    public int MaxSuggestions { get; set; } = 2;

    public IReadOnlyList<AiSkillMatchCandidateDto> Candidates { get; set; } = [];
}
