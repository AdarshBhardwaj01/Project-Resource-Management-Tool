namespace PRM.Models.DTOs.EmployeePortal;

public class EmployeeAllocationItemDto
{
    public string ProjectName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
