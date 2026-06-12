using PRM.Business.Helpers;

namespace PRM.Tests.Helpers;

public class SkillMatchHelperTests
{
    [Fact]
    public void MatchesAllSkillRequirements_WhenOnePersonHasAllRequestedSkills_ReturnsTrue()
    {
        var result = SkillMatchHelper.MatchesAllSkillRequirements(
            "DotNet, Java, React",
            ["dotnet", "java"]);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllSkillRequirements_WhenOnePersonMissesAnyRequestedSkill_ReturnsFalse()
    {
        var result = SkillMatchHelper.MatchesAllSkillRequirements(
            "DotNet, React",
            ["dotnet", "java"]);

        Assert.False(result);
    }
}
