using PRM.Business.Helpers;
using PRM.Models.DTOs.Ai;
using PRM.Models.DTOs.Manager;

namespace PRM.Business.Services.Ai;

internal static class SkillMatchResultBuilder
{

    public static SkillMatchResponse Build(AiSkillMatchContext context)
    {
        var parsed = ToRequirementParse(context);
        var skillKeywords = parsed.SkillKeywords;
        var ranked = context.Candidates
            .Where(candidate => SkillMatchHelper.IsEligibleCandidate(candidate, parsed))
            .Where(candidate =>
                !context.RequireSingleEmployeeMatch
                || skillKeywords.Count <= 1
                || SkillMatchHelper.MatchesAllSkillRequirements(candidate.Skills, skillKeywords))
            .Select(candidate => (
                Candidate: candidate,
                Score: SkillMatchHelper.ScoreEmployeeSkills(candidate.Skills, skillKeywords)))
            .Where(item => item.Score > 0 || skillKeywords.Count == 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => SkillMatchHelper.GetAvailablePercent(item.Candidate))
            .ThenByDescending(item => item.Candidate.IsOnBench)
            .Take(Math.Max(1, context.MaxSuggestions))
            .ToList();
        if (ranked.Count == 0)
        {
            return new SkillMatchResponse
            {
                NoMatchReason = context.RequireSingleEmployeeMatch
                    ? SkillMatchHelper.BuildSingleEmployeeNoMatchReason(parsed, true)
                    : SkillMatchHelper.BuildNoMatchReason(parsed)
            };
        }
        return new SkillMatchResponse
        {
            Suggestions = ranked
                .Select((item, index) => RuleBasedAiFallback.BuildSuggestion(
                    item.Candidate,
                    index + 1,
                    parsed))
                .ToList()
        };
    }

    public static SkillMatchResponse? ValidateLlmResponse(
        SkillMatchResponse? response,
        AiSkillMatchContext context)
    {
        if (response is null || response.Suggestions.Count == 0)
        {
            return null;
        }
        var parsed = ToRequirementParse(context);
        var skillKeywords = parsed.SkillKeywords;
        var candidateLookup = context.Candidates.ToDictionary(candidate => candidate.EmployeeId);
        var validated = new List<SkillMatchSuggestionDto>();
        var rowNumber = 1;
        foreach (var suggestion in response.Suggestions)
        {
            if (!candidateLookup.TryGetValue(suggestion.EmployeeId, out var candidate))
            {
                continue;
            }
            if (!SkillMatchHelper.IsEligibleCandidate(candidate, parsed))
            {
                continue;
            }
            if (context.RequireSingleEmployeeMatch
                && skillKeywords.Count > 1
                && !SkillMatchHelper.MatchesAllSkillRequirements(candidate.Skills, skillKeywords))
            {
                continue;
            }
            suggestion.RowNumber = rowNumber++;
            RuleBasedAiFallback.EnrichSuggestion(suggestion, candidate, parsed);
            validated.Add(suggestion);
        }
        if (context.RequireSingleEmployeeMatch && validated.Count > 1)
        {
            validated = validated.Take(1).ToList();
        }
        return validated.Count == 0
            ? null
            : new SkillMatchResponse { Suggestions = validated };
    }

    private static SkillMatchRequirementParse ToRequirementParse(AiSkillMatchContext context) =>
        SkillMatchHelper.ParseRequirement(context.Requirement);
}
