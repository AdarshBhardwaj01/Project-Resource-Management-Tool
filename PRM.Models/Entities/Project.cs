using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public ProjectStatus Status { get; set; }

    public ProjectHealthStatus HealthStatus { get; set; } = ProjectHealthStatus.OnTrack;

    public DateTime? AtRiskNotificationSentAt { get; set; }

    public int ManagerId { get; set; }

    public User Manager { get; set; } = null!;

    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();

    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();
}
