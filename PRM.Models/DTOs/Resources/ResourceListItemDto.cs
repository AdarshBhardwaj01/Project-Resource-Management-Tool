namespace PRM.Models.DTOs.Resources;

public class ResourceListItemDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
