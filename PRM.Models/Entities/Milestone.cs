using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class Milestone
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    public int SortOrder { get; set; }

    public Project Project { get; set; } = null!;
}
