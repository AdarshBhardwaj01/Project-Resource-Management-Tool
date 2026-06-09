namespace PRM.Models.DTOs.Projects;

public class MilestoneItemDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string DueDate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
