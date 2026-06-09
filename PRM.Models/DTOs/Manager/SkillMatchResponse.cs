namespace PRM.Models.DTOs.Manager;

public class SkillMatchResponse
{
    public IReadOnlyList<SkillMatchSuggestionDto> Suggestions { get; set; } = [];

    public string? NoMatchReason { get; set; }
}
