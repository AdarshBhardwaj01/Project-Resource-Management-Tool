namespace PRM.Models.DTOs.Employees;

public class AddEmployeeSkillRequest
{
    public string SkillName { get; set; } = string.Empty;

    public int Category { get; set; }

    public int ProficiencyLevel { get; set; }
}
