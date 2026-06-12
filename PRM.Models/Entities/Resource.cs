using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class Resource
{
    public int UserId { get; set; }

    public ResourceStatus Status { get; set; } = ResourceStatus.Bench;

    public int? ManagerUserId { get; set; }

    public int UtilisationPercent { get; set; }

    public User User { get; set; } = null!;

    public User? Manager { get; set; }

    public ICollection<ResourceSkill> Skills { get; set; } = new List<ResourceSkill>();

    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
