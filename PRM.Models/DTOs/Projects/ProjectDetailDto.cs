namespace PRM.Models.DTOs.Projects;

public class ProjectDetailDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string HealthStatus { get; set; } = string.Empty;

    public int ManagerId { get; set; }

    public string ManagerName { get; set; } = string.Empty;

    public List<MilestoneItemDto> Milestones { get; set; } = new();
}
