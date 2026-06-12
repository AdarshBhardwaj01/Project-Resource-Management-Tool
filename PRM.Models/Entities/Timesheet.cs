using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class Timesheet
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public TimesheetStatus Status { get; set; }

    public int TotalHours { get; set; }

    public Resource Resource { get; set; } = null!;

    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
