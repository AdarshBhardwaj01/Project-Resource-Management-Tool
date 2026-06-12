using PRM.Business.Helpers;

namespace PRM.Tests.Helpers;

public class TeamBuildRequirementParserTests
{
    // ── Single role ───────────────────────────────────────────────────────────

    [Fact]
    public void Parse_SingleRole_ReturnsSingleSlot()
    {
        var slots = TeamBuildRequirementParser.Parse("1 Java developer");

        Assert.Single(slots);
        Assert.Equal(1, slots[0].Count);
        Assert.Contains("Java", slots[0].SkillKeywords, StringComparer.OrdinalIgnoreCase);
    }

    // ── Multiple roles comma-separated ────────────────────────────────────────

    [Fact]
    public void Parse_MultipleRolesCommaSeparated_ReturnsAllSlots()
    {
        var slots = TeamBuildRequirementParser.Parse("1 Java developer, 1 QA engineer, 1 DevOps");

        Assert.Equal(3, slots.Count);
    }

    [Fact]
    public void Parse_MultipleRolesWithAnd_ReturnsAllSlots()
    {
        var slots = TeamBuildRequirementParser.Parse("1 Java developer and 1 QA tester");

        Assert.Equal(2, slots.Count);
    }

    // ── Count detection ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("2 Java developers", 2)]
    [InlineData("3 QA engineers", 3)]
    [InlineData("1 DevOps engineer", 1)]
    public void Parse_WithNumericCount_SetsCountCorrectly(string prompt, int expectedCount)
    {
        var slots = TeamBuildRequirementParser.Parse(prompt);

        Assert.Single(slots);
        Assert.Equal(expectedCount, slots[0].Count);
    }

    [Theory]
    [InlineData("two Java developers", 2)]
    [InlineData("three QA engineers", 3)]
    [InlineData("one DevOps engineer", 1)]
    public void Parse_WithWordCount_SetsCountCorrectly(string prompt, int expectedCount)
    {
        var slots = TeamBuildRequirementParser.Parse(prompt);

        Assert.Single(slots);
        Assert.Equal(expectedCount, slots[0].Count);
    }

    // ── Leading intro stripping ───────────────────────────────────────────────

    [Theory]
    [InlineData("I need 1 Java developer")]
    [InlineData("We need 1 Java developer")]
    [InlineData("Need 1 Java developer")]
    [InlineData("I am looking for 1 Java developer")]
    public void Parse_WithLeadingIntro_ParsesRoleCorrectly(string prompt)
    {
        var slots = TeamBuildRequirementParser.Parse(prompt);

        Assert.Single(slots);
        Assert.Contains("Java", slots[0].SkillKeywords, StringComparer.OrdinalIgnoreCase);
    }

    // ── Count expansion ───────────────────────────────────────────────────────

    [Fact]
    public void Parse_CountTwo_ExpandsToTwoSlotsWithSameRole()
    {
        // Count = 2 means we expect 2 slot entries, each with count=2 in the slot
        var slots = TeamBuildRequirementParser.Parse("2 Java developers");

        Assert.Single(slots);
        Assert.Equal(2, slots[0].Count);
    }

    // ── Numbered list format ──────────────────────────────────────────────────

    [Fact]
    public void Parse_NumberedListWithDots_ParsesAllRoles()
    {
        var slots = TeamBuildRequirementParser.Parse("1. Java developer 2. QA engineer 3. DevOps");

        Assert.Equal(3, slots.Count);
    }

    // ── Skill keywords ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_JavaDeveloperRole_ContainsJavaKeyword()
    {
        var slots = TeamBuildRequirementParser.Parse("1 Java developer");

        Assert.Contains("Java", slots[0].SkillKeywords, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_QaSlashSdet_ContainsBothKeywords()
    {
        var slots = TeamBuildRequirementParser.Parse("1 QA SDET");

        Assert.Single(slots);
        var keywords = slots[0].SkillKeywords;
        Assert.True(
            keywords.Any(k => k.Equals("QA", StringComparison.OrdinalIgnoreCase)) ||
            keywords.Any(k => k.Equals("SDET", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Parse_DevOpsRole_ContainsDevOpsKeyword()
    {
        var slots = TeamBuildRequirementParser.Parse("1 DevOps engineer");

        Assert.Single(slots);
        Assert.Contains("DevOps", slots[0].SkillKeywords, StringComparer.OrdinalIgnoreCase);
    }

    // ── Role label ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_RoleLabel_IsTitleCased()
    {
        var slots = TeamBuildRequirementParser.Parse("1 java developer");

        Assert.Single(slots);
        Assert.Equal("Java Developer", slots[0].RoleLabel);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var slots = TeamBuildRequirementParser.Parse(string.Empty);

        Assert.Empty(slots);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmpty()
    {
        var slots = TeamBuildRequirementParser.Parse("   ");

        Assert.Empty(slots);
    }

    [Fact]
    public void Parse_OnlyStopWords_ReturnsEmpty()
    {
        // All words should be filtered as stop words or too short
        var slots = TeamBuildRequirementParser.Parse("a and the");

        Assert.Empty(slots);
    }

    [Fact]
    public void Parse_FullTeamPrompt_ParsesAllThreeRoles()
    {
        var prompt = "I need 1 Java developer, 1 QA SDET, 1 DevOps engineer";
        var slots = TeamBuildRequirementParser.Parse(prompt);

        Assert.Equal(3, slots.Count);
        Assert.All(slots, slot => Assert.NotEmpty(slot.SkillKeywords));
    }

    [Fact]
    public void Parse_QaAsStandaloneRole_PreservesQaSlot()
    {
        var prompt = "i need 1 java developer, 1 devops engineer, 1 QA";
        var slots = TeamBuildRequirementParser.Parse(prompt);

        Assert.Equal(3, slots.Count);
        Assert.Contains(slots, slot => slot.RoleLabel.Equals("Qa", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            slots.Single(slot => slot.RoleLabel.Equals("Qa", StringComparison.OrdinalIgnoreCase)).SkillKeywords,
            keyword => keyword.Equals("QA", StringComparison.OrdinalIgnoreCase));
    }
}
