namespace PRM.Models.DTOs.Manager;

public class AllocationValidationDto
{
    public string EmployeeName { get; set; } = string.Empty;

    public int CurrentUtilisation { get; set; }

    public int ProposedUtilisation { get; set; }

    public int TotalUtilisation { get; set; }

    public bool IsValid { get; set; }
}
