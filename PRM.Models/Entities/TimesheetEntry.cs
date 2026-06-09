namespace PRM.Models.Entities;

public class TimesheetEntry
{
    public int Id { get; set; }

    public int TimesheetId { get; set; }

    public int ProjectId { get; set; }

    public decimal Hours { get; set; }

    public string ActivityTags { get; set; } = string.Empty;

    public Timesheet Timesheet { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
