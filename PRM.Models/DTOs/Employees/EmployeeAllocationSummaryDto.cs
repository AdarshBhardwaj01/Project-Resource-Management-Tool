namespace PRM.Models.DTOs.Employees;

public class EmployeeAllocationSummaryDto
{
    public string ProjectName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string ToDate { get; set; } = string.Empty;
}
