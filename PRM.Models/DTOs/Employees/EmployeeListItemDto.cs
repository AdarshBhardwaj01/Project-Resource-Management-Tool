namespace PRM.Models.DTOs.Employees;

public class EmployeeListItemDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
