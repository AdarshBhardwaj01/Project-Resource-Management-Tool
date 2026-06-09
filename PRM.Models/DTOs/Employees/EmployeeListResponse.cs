namespace PRM.Models.DTOs.Employees;

public class EmployeeListResponse
{
    public List<EmployeeListItemDto> Employees { get; set; } = new();

    public int Total { get; set; }

    public int AllocatedCount { get; set; }

    public int BenchCount { get; set; }
}
