namespace PRM.Models.DTOs.Manager;

public class ManagerProjectItemDto
{
    public int Id { get; set; }

    public int RowNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public string HealthStatus { get; set; } = string.Empty;
}