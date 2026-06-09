namespace PRM.Models.DTOs.Projects;

public class CreateMilestoneRequest
{
    public string Title { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public int SortOrder { get; set; }
}
