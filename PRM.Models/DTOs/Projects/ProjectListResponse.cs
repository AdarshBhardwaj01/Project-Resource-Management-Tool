namespace PRM.Models.DTOs.Projects;

public class ProjectListResponse
{
    public List<ProjectListItemDto> Projects { get; set; } = new();

    public int Total { get; set; }

    public int PlannedCount { get; set; }

    public int ActiveCount { get; set; }

    public int OnHoldCount { get; set; }
}
