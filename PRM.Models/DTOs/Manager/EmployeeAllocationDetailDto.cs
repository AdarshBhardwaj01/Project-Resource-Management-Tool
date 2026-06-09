namespace PRM.Models.DTOs.Manager;

public class EmployeeAllocationDetailDto
{
    public string ProjectName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;
}
