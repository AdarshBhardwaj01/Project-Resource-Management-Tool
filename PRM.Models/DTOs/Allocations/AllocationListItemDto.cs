namespace PRM.Models.DTOs.Allocations;

public class AllocationListItemDto
{
    public int Id { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
