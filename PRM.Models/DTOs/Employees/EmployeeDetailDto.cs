namespace PRM.Models.DTOs.Employees;

public class EmployeeDetailDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Designation { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public List<EmployeeAllocationSummaryDto> ActiveAllocations { get; set; } = new();
}
