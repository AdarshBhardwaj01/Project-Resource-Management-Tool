namespace PRM.Models.DTOs.Ai;

public class AiSkillMatchContext
{
    public string Requirement { get; set; } = string.Empty;

    public string? ProjectName { get; set; }

    public int? MinAvailablePercent { get; set; }

    public DateTime? AvailableFromDate { get; set; }

    public bool RequireFullAvailability { get; set; }

    public IReadOnlyList<AiSkillMatchCandidateDto> Candidates { get; set; } = [];
}
