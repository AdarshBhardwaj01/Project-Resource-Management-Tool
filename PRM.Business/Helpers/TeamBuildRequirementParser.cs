using System.Text.RegularExpressions;

namespace PRM.Business.Helpers;

internal static class TeamBuildRequirementParser
{
    private static readonly Regex LeadingIntroPattern = new(
        @"^\s*(?:i\s+)?(?:need|want|require|am\s+looking\s+for|looking\s+for)\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NumberedBulletPattern = new(
        @"(?<!\d)\d+\s*[.)]\s*",
        RegexOptions.Compiled);

    private static readonly Regex CountPrefixPattern = new(
        @"^(?:(\d+)|(\bone\b|\btwo\b|\bthree\b|\bfour\b|\bfive\b|\bsix\b|\bseven\b|\beight\b|\bnine\b|\bten\b|\ban?\b))\s+(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, int> WordToCount = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = 1, ["an"] = 1, ["one"] = 1,
        ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
        ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10
    };

    public static IReadOnlyList<TeamRoleSlot> Parse(string prompt)
    {
        var cleaned = LeadingIntroPattern.Replace(prompt.Trim(), string.Empty);
        var segments = SplitIntoSegments(cleaned);
        var slots = new List<TeamRoleSlot>();
        foreach (var segment in segments)
        {
            var trimmed = segment.Trim(' ', '.', ',', ';');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }
            var slot = ParseSegment(trimmed);
            if (slot is not null)
            {
                slots.Add(slot);
            }
        }
        return slots;
    }

    private static IReadOnlyList<string> SplitIntoSegments(string text)
    {
        // Handle numbered list items first: "1. Java dev 2. QA 3. DevOps"
        if (NumberedBulletPattern.IsMatch(text))
        {
            return NumberedBulletPattern.Split(text)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();
        }
        // Split on commas and " and "
        return Regex.Split(text, @",|\band\b", RegexOptions.IgnoreCase)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();
    }

    private static TeamRoleSlot? ParseSegment(string segment)
    {
        var count = 1;
        var roleText = segment;
        var match = CountPrefixPattern.Match(segment);
        if (match.Success)
        {
            if (match.Groups[1].Success && int.TryParse(match.Groups[1].Value, out var numeric))
            {
                count = Math.Max(1, numeric);
            }
            else if (match.Groups[2].Success && WordToCount.TryGetValue(match.Groups[2].Value, out var wordCount))
            {
                count = wordCount;
            }
            roleText = match.Groups[3].Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(roleText))
        {
            return null;
        }
        var keywords = SkillMatchHelper.ExtractSkillKeywords(roleText);
        if (keywords.Count == 0)
        {
            return null;
        }
        return new TeamRoleSlot
        {
            Count = count,
            RoleLabel = ToTitleCase(roleText),
            SkillKeywords = keywords
        };
    }

    private static string ToTitleCase(string value)
    {
        return string.Join(" ",
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                 .Select(word => word.Length == 0
                     ? word
                     : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }
}
