namespace PRM.Business.Helpers;

internal sealed class TeamRoleSlot
{
    public int Count { get; init; } = 1;
    public string RoleLabel { get; init; } = string.Empty;
    public IReadOnlyList<string> SkillKeywords { get; init; } = [];
}
