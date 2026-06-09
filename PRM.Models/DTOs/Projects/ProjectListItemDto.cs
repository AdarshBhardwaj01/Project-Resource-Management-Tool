namespace PRM.Models.DTOs.Projects;

public class ProjectListItemDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ManagerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public int MilestoneCount { get; set; }
}
