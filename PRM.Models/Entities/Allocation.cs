namespace PRM.Models.Entities;

public class Allocation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProjectId { get; set; }

    public int UtilisationPercent { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public Resource Resource { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
