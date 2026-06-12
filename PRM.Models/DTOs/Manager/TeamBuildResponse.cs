namespace PRM.Models.DTOs.Manager;

public class TeamBuildResponse
{
    public IReadOnlyList<TeamRoleResultDto> Roles { get; set; } = [];
    public int FilledCount { get; set; }
    public int GapCount { get; set; }
}
