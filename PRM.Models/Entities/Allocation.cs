namespace PRM.Models.Entities;

public class Allocation
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int ProjectId { get; set; }

    public int UtilisationPercent { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public Employee Employee { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
