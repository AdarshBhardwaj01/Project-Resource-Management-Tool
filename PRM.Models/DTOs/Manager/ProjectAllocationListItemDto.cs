namespace PRM.Models.DTOs.Manager;

public class ProjectAllocationListItemDto
{
    public int Id { get; set; }

    public int RowNumber { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;
}
