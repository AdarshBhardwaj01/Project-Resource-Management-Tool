using System.Globalization;
using System.Text.RegularExpressions;
using PRM.Models.DTOs.Ai;
using PRM.Models.Entities;

namespace PRM.Business.Helpers;

public static class SkillMatchHelper
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "with", "who", "can", "for", "in", "on", "to", "of", "we", "need",
        "is", "are", "be", "from", "any", "new", "project", "months", "month", "join", "experience", "developer",
        "employee", "having", "have", "has", "knowledge", "some", "idea", "also", "least", "day", "days",
        "atleast", "resource", "type", "kind", "must", "should", "would", "could", "please", "that", "this",
        "fully", "available", "availability", "free", "bench", "full", "partial", "about", "percent", "percentage",
        "starting", "beginning", "january", "february", "march", "april", "may", "june", "july", "august",
        "september", "october", "november", "december", "jan", "feb", "mar", "apr", "jun", "jul", "aug",
        "sep", "sept", "oct", "nov", "dec", "th", "st", "nd", "rd"
    };

    private static readonly Regex MinAvailablePercentPattern = new(
        @"(?:availability|available)\s*(?:is|of|at|:)?\s*(\d{1,3})\s*(?:%|percent)|" +
        @"(\d{1,3})\s*(?:%|percent)\s*(?:availability|available|free)|" +
        @"(?:at\s*least|minimum|min)\s*(\d{1,3})\s*(?:%|percent)\s*(?:free|available|availability)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AvailableFromDayMonthPattern = new(
        @"(?:from|starting|beginning)\s+(\d{1,2})(?:st|nd|rd|th)?\s+" +
        @"(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|" +
        @"sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)(?:\s+(\d{4}))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AvailableFromMonthDayPattern = new(
        @"(?:from|starting|beginning)\s+" +
        @"(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|" +
        @"sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+(\d{1,2})(?:st|nd|rd|th)?(?:\s+(\d{4}))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AvailableFromIsoDatePattern = new(
        @"(?:from|starting|beginning)\s+(\d{2}[-/]\d{2}[-/]\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SkillMatchRequirementParse ParseRequirement(string requirement)
    {
        var requireFullAvailability = RequiresFullAvailability(requirement);
        var minAvailablePercent = TryParseMinAvailablePercent(requirement);
        var availableFromDate = TryParseAvailableFromDate(requirement);
        if (requireFullAvailability && !minAvailablePercent.HasValue)
        {
            minAvailablePercent = 100;
        }
        var skillSource = RemoveParsedAvailabilityPhrases(requirement);
        var skillKeywords = ExtractSkillKeywords(skillSource);
        return new SkillMatchRequirementParse
        {
            SkillKeywords = skillKeywords,
            MinAvailablePercent = minAvailablePercent,
            AvailableFromDate = availableFromDate,
            RequireFullAvailability = requireFullAvailability
        };
    }

    public static bool HasAssignedSkills(Resource resource) => resource.Skills.Count > 0;

    public static bool HasAssignedSkills(string skillsDisplay) =>
        !string.IsNullOrWhiteSpace(skillsDisplay) &&
        !skillsDisplay.Equals("(none)", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> ExtractRequirementKeywords(string requirement) =>
        ExtractTokens(requirement);

    public static IReadOnlyList<string> ExtractSkillKeywords(string requirement) =>
        ExtractTokens(requirement)
            .Where(keyword => !StopWords.Contains(keyword))
            .Where(keyword => !int.TryParse(keyword, out _))
            .ToList();

    public static bool RequiresFullAvailability(string requirement)
    {
        var normalized = NormalizeToken(requirement);
        return normalized.Contains("fullyavailable")
            || normalized.Contains("fullavailability")
            || normalized.Contains("100free")
            || normalized.Contains("onbench")
            || normalized.Contains("fullyonbench")
            || (normalized.Contains("fully") && normalized.Contains("available"));
    }

    public static int? TryParseMinAvailablePercent(string requirement)
    {
        var match = MinAvailablePercentPattern.Match(requirement);
        if (!match.Success)
        {
            return null;
        }
        for (var groupIndex = 1; groupIndex <= 3; groupIndex++)
        {
            var value = match.Groups[groupIndex].Value;
            if (int.TryParse(value, out var percent) && percent is > 0 and <= 100)
            {
                return percent;
            }
        }
        return null;
    }

    public static DateTime? TryParseAvailableFromDate(string requirement)
    {
        var dayMonthMatch = AvailableFromDayMonthPattern.Match(requirement);
        if (dayMonthMatch.Success
            && int.TryParse(dayMonthMatch.Groups[1].Value, out var day)
            && TryParseMonth(dayMonthMatch.Groups[2].Value, out var month))
        {
            var year = ParseYearOrDefault(dayMonthMatch.Groups[3].Value, month, day);
            return new DateTime(year, month, day);
        }
        var monthDayMatch = AvailableFromMonthDayPattern.Match(requirement);
        if (monthDayMatch.Success
            && int.TryParse(monthDayMatch.Groups[2].Value, out day)
            && TryParseMonth(monthDayMatch.Groups[1].Value, out month))
        {
            var year = ParseYearOrDefault(monthDayMatch.Groups[3].Value, month, day);
            return new DateTime(year, month, day);
        }
        var isoMatch = AvailableFromIsoDatePattern.Match(requirement);
        if (isoMatch.Success)
        {
            var formats = new[] { "dd-MM-yyyy", "dd/MM/yyyy" };
            var raw = isoMatch.Groups[1].Value.Replace('/', '-');
            if (DateTime.TryParseExact(
                    raw,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                return parsedDate.Date;
            }
        }
        return null;
    }

    public static bool MatchesSkillRequirement(string skillsDisplay, IReadOnlyList<string> skillKeywords)
    {
        if (skillKeywords.Count == 0)
        {
            return true;
        }
        if (!HasAssignedSkills(skillsDisplay))
        {
            return false;
        }
        var skillNames = SplitSkillNames(skillsDisplay);
        return skillKeywords.Any(keyword =>
            skillNames.Any(skill => SkillMatchesKeyword(skill, keyword)));
    }

    public static bool MatchesAllSkillRequirements(string skillsDisplay, IReadOnlyList<string> skillKeywords)
    {
        if (skillKeywords.Count == 0)
        {
            return true;
        }
        if (!HasAssignedSkills(skillsDisplay))
        {
            return false;
        }
        var skillNames = SplitSkillNames(skillsDisplay);
        return skillKeywords.All(keyword =>
            skillNames.Any(skill => SkillMatchesKeyword(skill, keyword)));
    }

    public static bool IsEligibleCandidate(
        AiSkillMatchCandidateDto candidate,
        SkillMatchRequirementParse parsed)
    {
        if (!MatchesSkillRequirement(candidate.Skills, parsed.SkillKeywords))
        {
            return false;
        }
        var availablePercent = GetAvailablePercent(candidate);
        var requiredPercent = parsed.RequiredAvailablePercent;
        if (requiredPercent > 0 && availablePercent < requiredPercent)
        {
            return false;
        }
        return true;
    }

    public static bool IsEligibleCandidate(
        AiSkillMatchCandidateDto candidate,
        IReadOnlyList<string> skillKeywords,
        bool requireFullAvailability)
    {
        return IsEligibleCandidate(
            candidate,
            new SkillMatchRequirementParse
            {
                SkillKeywords = skillKeywords,
                RequireFullAvailability = requireFullAvailability,
                MinAvailablePercent = requireFullAvailability ? 100 : null
            });
    }

    public static bool IsFullyAvailable(AiSkillMatchCandidateDto candidate) =>
        GetAvailablePercent(candidate) >= 100;

    public static int GetAvailablePercent(AiSkillMatchCandidateDto candidate)
    {
        if (candidate.UtilisationPercent >= 100)
        {
            return 0;
        }
        if (candidate.IsOnBench && candidate.UtilisationPercent == 0)
        {
            return 100;
        }
        return Math.Max(0, 100 - candidate.UtilisationPercent);
    }

    public static string FormatAvailabilityForDate(
        int usedPercent,
        bool isOnBench,
        DateTime? availableFromDate)
    {
        var availablePercent = usedPercent >= 100 ? 0 : Math.Max(0, 100 - usedPercent);
        var availabilityText = availablePercent switch
        {
            100 when isOnBench || usedPercent == 0 => "100% free",
            100 => "100% free",
            0 => "0% free",
            _ => $"{availablePercent}% free"
        };
        if (!availableFromDate.HasValue)
        {
            return availabilityText;
        }
        return $"{availabilityText} from {availableFromDate.Value:dd-MMM-yyyy}";
    }

    public static string BuildNoMatchReason(SkillMatchRequirementParse parsed)
    {
        if (parsed.SkillKeywords.Count > 0 && parsed.HasAvailabilityConstraint)
        {
            if (parsed.AvailableFromDate.HasValue)
            {
                return
                    $"No employees on your team match the required skills with at least " +
                    $"{parsed.RequiredAvailablePercent}% availability from " +
                    $"{parsed.AvailableFromDate.Value:dd-MMM-yyyy}.";
            }
            return
                $"No employees on your team match the required skills with at least " +
                $"{parsed.RequiredAvailablePercent}% availability.";
        }
        if (parsed.HasAvailabilityConstraint)
        {
            if (parsed.AvailableFromDate.HasValue)
            {
                return
                    $"No employees on your team have at least {parsed.RequiredAvailablePercent}% availability " +
                    $"from {parsed.AvailableFromDate.Value:dd-MMM-yyyy}.";
            }
            return
                $"No employees on your team have at least {parsed.RequiredAvailablePercent}% availability.";
        }
        return "No matching employees with the required skills were found on your team.";
    }

    public static string FormatMatchedSkills(string skillsDisplay, IReadOnlyList<string> skillKeywords)
    {
        var skillNames = SplitSkillNames(skillsDisplay);
        if (skillNames.Count == 0)
        {
            return skillsDisplay;
        }
        if (skillKeywords.Count == 0)
        {
            return string.Join(", ", skillNames.Take(2));
        }
        var matched = skillNames
            .Where(skill => skillKeywords.Any(keyword => SkillMatchesKeyword(skill, keyword)))
            .ToList();
        return matched.Count == 0 ? string.Empty : string.Join(", ", matched);
    }

    public static string FormatRecentActivity(Resource resource, IReadOnlyList<string> skillKeywords)
    {
        var fourWeeksAgo = DateTime.UtcNow.Date.AddDays(-28);
        var tags = resource.Timesheets
            .Where(timesheet => timesheet.WeekStartDate.Date >= fourWeeksAgo)
            .SelectMany(timesheet => timesheet.Entries)
            .SelectMany(entry => entry.ActivityTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tags.Count == 0)
        {
            return "(none)";
        }
        if (skillKeywords.Count > 0)
        {
            var matchedTag = tags.FirstOrDefault(tag =>
                skillKeywords.Any(keyword => SkillMatchesKeyword(tag, keyword)));
            if (matchedTag is not null)
            {
                return $"{matchedTag} ✓";
            }
        }
        return "(none)";
    }

    public static string FormatTableAvailability(bool isOnBench, int usedPercent, string availability)
    {
        if (isOnBench && usedPercent == 0)
        {
            return availability.Contains("from", StringComparison.OrdinalIgnoreCase)
                ? availability
                : "100% free";
        }
        if (usedPercent >= 100)
        {
            return availability.Contains("from", StringComparison.OrdinalIgnoreCase)
                ? availability
                : "0% free";
        }
        if (usedPercent == 0)
        {
            return availability.Contains("from", StringComparison.OrdinalIgnoreCase)
                ? availability
                : "100% free";
        }
        if (availability.Contains('%'))
        {
            return availability;
        }
        return $"{100 - usedPercent}% free";
    }

    public static int ScoreEmployeeSkills(string skillsDisplay, IReadOnlyList<string> skillKeywords)
    {
        if (!HasAssignedSkills(skillsDisplay) || skillKeywords.Count == 0)
        {
            return 0;
        }
        var skillNames = SplitSkillNames(skillsDisplay);
        return skillKeywords.Count(keyword =>
            skillNames.Any(skill => SkillMatchesKeyword(skill, keyword)));
    }

    public static string BuildSingleEmployeeNoMatchReason(
        SkillMatchRequirementParse parsed,
        bool searchEntireOrganization)
    {
        var scope = searchEntireOrganization ? "the organization" : "your team";
        if (parsed.SkillKeywords.Count > 1)
        {
            return $"No single employee in {scope} matches all the required skills together.";
        }
        return $"No single employee in {scope} matches the required skill.";
    }

    public static bool SkillMatchesKeyword(string skill, string keyword)
    {
        var normalizedSkill = NormalizeToken(skill);
        var normalizedKeyword = NormalizeToken(keyword);
        if (string.IsNullOrWhiteSpace(normalizedSkill) || string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return false;
        }
        if (normalizedSkill.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
            || normalizedKeyword.Contains(normalizedSkill, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return normalizedKeyword.Length >= 4
            && normalizedSkill.StartsWith(normalizedKeyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveParsedAvailabilityPhrases(string requirement)
    {
        var cleaned = requirement;
        cleaned = MinAvailablePercentPattern.Replace(cleaned, " ");
        cleaned = AvailableFromDayMonthPattern.Replace(cleaned, " ");
        cleaned = AvailableFromMonthDayPattern.Replace(cleaned, " ");
        cleaned = AvailableFromIsoDatePattern.Replace(cleaned, " ");
        return cleaned;
    }

    private static IReadOnlyList<string> ExtractTokens(string requirement)
    {
        return requirement
            .Split([' ', ',', '.', ';', ':', '/', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.Length > 2 || IsShortSkillAcronym(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> SplitSkillNames(string skillsDisplay) =>
        skillsDisplay
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static string NormalizeToken(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray());

    private static bool IsShortSkillAcronym(string word)
    {
        var normalized = NormalizeToken(word);
        return normalized.Length >= 2
            && normalized.Length <= 4
            && normalized.All(char.IsLetter)
            && normalized.Any(char.IsUpper);
    }

    private static bool TryParseMonth(string monthToken, out int month)
    {
        month = monthToken.ToLowerInvariant() switch
        {
            "jan" or "january" => 1,
            "feb" or "february" => 2,
            "mar" or "march" => 3,
            "apr" or "april" => 4,
            "may" => 5,
            "jun" or "june" => 6,
            "jul" or "july" => 7,
            "aug" or "august" => 8,
            "sep" or "sept" or "september" => 9,
            "oct" or "october" => 10,
            "nov" or "november" => 11,
            "dec" or "december" => 12,
            _ => 0
        };
        return month > 0;
    }

    private static int ParseYearOrDefault(string yearToken, int month, int day)
    {
        if (int.TryParse(yearToken, out var year) && year is >= 2000 and <= 2100)
        {
            return year;
        }
        var today = DateTime.UtcNow.Date;
        var candidate = new DateTime(today.Year, month, day);
        return candidate.Date < today ? today.Year + 1 : today.Year;
    }
}
