namespace PRM.Models.DTOs.Manager;

public class ManagerProjectDetailDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string HealthStatus { get; set; } = string.Empty;

    public IReadOnlyList<ManagerProjectRiskFlagDto> RiskFlags { get; set; } = [];

    public IReadOnlyList<ManagerProjectMilestoneDto> Milestones { get; set; } = [];

    public IReadOnlyList<ManagerProjectAllocationDto> Allocations { get; set; } = [];
}

public class ManagerProjectRiskFlagDto
{
    public bool IsPositive { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class ManagerProjectMilestoneDto
{
    public int RowNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string DueDate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class ManagerProjectAllocationDto
{
    public string EmployeeName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;
}
