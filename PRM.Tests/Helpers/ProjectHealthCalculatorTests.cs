using PRM.Business.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Tests.Helpers;

public class ProjectHealthCalculatorTests
{
    [Fact]
    public void ComputeForProject_OverdueMilestone_ReturnsAtRisk()
    {
        var today = new DateTime(2026, 6, 12);
        var project = new Project
        {
            Milestones =
            [
                new Milestone
                {
                    Title = "Design Document",
                    DueDate = new DateTime(2026, 6, 5),
                    Status = MilestoneStatus.NotStarted
                }
            ]
        };

        var health = ProjectHealthCalculator.ComputeForProject(project, today, maxWeeklyHours: 40);

        Assert.Equal(ProjectHealthStatus.AtRisk, health);
    }

    [Fact]
    public void ComputeForProject_CompletedOverdueMilestone_ReturnsOnTrack()
    {
        var today = new DateTime(2026, 6, 12);
        var project = new Project
        {
            Milestones =
            [
                new Milestone
                {
                    Title = "Design Document",
                    DueDate = new DateTime(2026, 6, 5),
                    Status = MilestoneStatus.Done
                }
            ]
        };

        var health = ProjectHealthCalculator.ComputeForProject(project, today, maxWeeklyHours: 40);

        Assert.Equal(ProjectHealthStatus.OnTrack, health);
    }
}
