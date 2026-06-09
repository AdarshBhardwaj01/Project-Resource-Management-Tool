namespace PRM.Models.DTOs.Manager;

public class CreateAllocationRequest
{
    public int EmployeeId { get; set; }

    public int ProjectId { get; set; }

    public int UtilisationPercent { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }
}
