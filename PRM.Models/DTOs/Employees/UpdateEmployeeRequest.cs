namespace PRM.Models.DTOs.Employees;

public class UpdateEmployeeRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Designation { get; set; } = string.Empty;
}
