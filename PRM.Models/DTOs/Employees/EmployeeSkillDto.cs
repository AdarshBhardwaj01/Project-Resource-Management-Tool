namespace PRM.Models.DTOs.Employees;

public class EmployeeSkillDto
{
    public int SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ProficiencyLevel { get; set; } = string.Empty;
}
