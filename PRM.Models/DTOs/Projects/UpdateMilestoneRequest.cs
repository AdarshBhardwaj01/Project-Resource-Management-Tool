namespace PRM.Models.DTOs.Projects;

public class UpdateMilestoneRequest
{
    public string Title { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public int Status { get; set; }

    public int SortOrder { get; set; }
}
