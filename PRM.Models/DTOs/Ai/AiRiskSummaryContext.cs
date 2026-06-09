namespace PRM.Models.DTOs.Ai;

public class AiRiskSummaryContext
{
    public string ProjectName { get; set; } = string.Empty;

    public string HealthStatus { get; set; } = string.Empty;

    public IReadOnlyList<string> Milestones { get; set; } = [];

    public IReadOnlyList<string> Allocations { get; set; } = [];

    public IReadOnlyList<string> RiskFlags { get; set; } = [];
}
