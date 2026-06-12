using System.Text.Json;
using System.Text.Json.Serialization;
using PRM.Business.Helpers;
using PRM.Models.DTOs.Ai;
using PRM.Models.DTOs.Manager;

namespace PRM.Business.Services.Ai;

internal static class AiResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static SkillMatchResponse? TryParseSkillMatch(
        string llmText,
        AiSkillMatchContext context)
    {
        if (string.IsNullOrWhiteSpace(llmText))
        {
            return null;
        }
        var json = ExtractJsonObject(llmText);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        LlmSkillMatchPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LlmSkillMatchPayload>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        if (payload?.Suggestions is null || payload.Suggestions.Count == 0)
        {
            return null;
        }
        var candidateLookup = context.Candidates.ToDictionary(candidate => candidate.EmployeeId);
        var parsed = SkillMatchHelper.ParseRequirement(context.Requirement);
        var suggestions = new List<SkillMatchSuggestionDto>();
        var rowNumber = 1;
        foreach (var item in payload.Suggestions.Take(Math.Max(1, context.MaxSuggestions)))
        {
            if (!candidateLookup.TryGetValue(item.EmployeeId, out var candidate))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.Reason))
            {
                continue;
            }
            if (!SkillMatchHelper.IsEligibleCandidate(candidate, parsed))
            {
                continue;
            }
            if (context.RequireSingleEmployeeMatch
                && parsed.SkillKeywords.Count > 1
                && !SkillMatchHelper.MatchesAllSkillRequirements(candidate.Skills, parsed.SkillKeywords))
            {
                continue;
            }
            suggestions.Add(new SkillMatchSuggestionDto
            {
                RowNumber = rowNumber++,
                EmployeeId = candidate.EmployeeId,
                EmployeeName = candidate.FullName,
                Reason = item.Reason.Trim()
            });
            RuleBasedAiFallback.EnrichSuggestion(suggestions[^1], candidate, parsed);
        }
        if (context.RequireSingleEmployeeMatch && suggestions.Count > 1)
        {
            suggestions = suggestions.Take(1).ToList();
        }
        return suggestions.Count == 0 ? null : new SkillMatchResponse { Suggestions = suggestions };
    }

    public static string? TryParsePlainText(string llmText)
    {
        if (string.IsNullOrWhiteSpace(llmText))
        {
            return null;
        }
        var cleaned = llmText.Trim().Trim('"');
        return cleaned.Length < 20 ? null : cleaned;
    }

    private static string ExtractJsonObject(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return trimmed[firstBrace..(lastBrace + 1)];
            }
        }
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }
        return string.Empty;
    }

    private sealed class LlmSkillMatchPayload
    {
        [JsonPropertyName("suggestions")]
        public List<LlmSkillMatchItem>? Suggestions { get; set; }
    }

    private sealed class LlmSkillMatchItem
    {
        [JsonPropertyName("employeeId")]
        public int EmployeeId { get; set; }
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
