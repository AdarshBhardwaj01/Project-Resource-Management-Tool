namespace PRM.Business.Helpers;

public sealed class SkillMatchRequirementParse
{
    public IReadOnlyList<string> SkillKeywords { get; init; } = [];

    public int? MinAvailablePercent { get; init; }

    public DateTime? AvailableFromDate { get; init; }

    public bool RequireFullAvailability { get; init; }

    public bool HasAvailabilityConstraint =>
        RequireFullAvailability || MinAvailablePercent is > 0;

    public int RequiredAvailablePercent
    {
        get
        {
            if (RequireFullAvailability)
            {
                return 100;
            }
            return MinAvailablePercent ?? 0;
        }
    }
}
