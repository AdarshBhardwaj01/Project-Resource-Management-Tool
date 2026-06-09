using PRM.Business.Helpers;

using PRM.Models.DTOs.Ai;

using PRM.Models.DTOs.Manager;



namespace PRM.Business.Services.Ai;



internal static class RuleBasedAiFallback

{

    public static SkillMatchResponse BuildSkillMatch(AiSkillMatchContext context) =>

        SkillMatchResultBuilder.Build(context);



    public static string BuildRiskSummary(AiRiskSummaryContext context)

    {

        var negativeFlags = context.RiskFlags.ToList();



        if (negativeFlags.Count == 0)

        {

            return

                "The project is currently on track with no major risk indicators. " +

                "Continue monitoring milestone progress and weekly timesheet submissions.";

        }



        var issueSentences = negativeFlags

            .Select(message =>

            {

                if (message.Contains("overdue", StringComparison.OrdinalIgnoreCase))

                {

                    return message.Replace(" milestone is", " milestone is currently", StringComparison.OrdinalIgnoreCase) + ".";

                }



                if (message.Contains("logged only", StringComparison.OrdinalIgnoreCase))

                {

                    return $"{message.ToLowerInvariant()}.";

                }



                return $"{message}.";

            })

            .ToList();



        var body = string.Join(" ", issueSentences);

        var recommendation =

            "Immediate action is recommended: a direct conversation with affected team members to identify blockers, " +

            "and a realistic assessment of whether remaining milestones can still be met on schedule.";



        return $"{body} {recommendation}";

    }



    public static SkillMatchSuggestionDto BuildSuggestion(

        AiSkillMatchCandidateDto candidate,

        int rowNumber,

        SkillMatchRequirementParse parsed)

    {

        var skillKeywords = parsed.SkillKeywords;

        var matchedSkills = SkillMatchHelper.FormatMatchedSkills(candidate.Skills, skillKeywords);



        return new SkillMatchSuggestionDto

        {

            RowNumber = rowNumber,

            EmployeeId = candidate.EmployeeId,

            EmployeeName = candidate.FullName,

            Reason = BuildSkillMatchReason(candidate, parsed, matchedSkills),

            SkillsMatch = matchedSkills,

            Availability = SkillMatchHelper.FormatTableAvailability(

                candidate.IsOnBench,

                candidate.UtilisationPercent,

                candidate.Availability),

            RecentActivity = candidate.RecentActivity

        };

    }



    public static void EnrichSuggestion(

        SkillMatchSuggestionDto suggestion,

        AiSkillMatchCandidateDto candidate,

        SkillMatchRequirementParse parsed)

    {

        var skillKeywords = parsed.SkillKeywords;

        suggestion.SkillsMatch = SkillMatchHelper.FormatMatchedSkills(candidate.Skills, skillKeywords);

        suggestion.Availability = SkillMatchHelper.FormatTableAvailability(

            candidate.IsOnBench,

            candidate.UtilisationPercent,

            candidate.Availability);

        suggestion.RecentActivity = candidate.RecentActivity;

        suggestion.Reason = BuildSkillMatchReason(candidate, parsed, suggestion.SkillsMatch);

    }



    private static string BuildSkillMatchReason(

        AiSkillMatchCandidateDto candidate,

        SkillMatchRequirementParse parsed,

        string matchedSkills)

    {

        var availablePercent = SkillMatchHelper.GetAvailablePercent(candidate);

        var availabilityText = candidate.Availability;



        if (parsed.HasAvailabilityConstraint)

        {

            if (parsed.SkillKeywords.Count == 0)

            {

                return

                    $"Has {availabilityText} and meets the {parsed.RequiredAvailablePercent}% availability requirement.";

            }



            var skillText = !string.IsNullOrWhiteSpace(matchedSkills)

                ? $"{matchedSkills} expertise"

                : $"{candidate.Skills} background";



            return

                $"{skillText}; {availabilityText}; meets the {parsed.RequiredAvailablePercent}% availability requirement.";

        }



        var defaultSkillText = !string.IsNullOrWhiteSpace(matchedSkills)

            ? $"{matchedSkills} expertise"

            : $"{candidate.Skills} background";



        if (candidate.IsOnBench && candidate.UtilisationPercent == 0)

        {

            return

                $"{defaultSkillText}; currently on bench and fully available from any date; " +

                "recent activity can be verified before allocation.";

        }



        return

            $"{defaultSkillText}; currently {availablePercent}% free and available for a new allocation.";

    }

}


