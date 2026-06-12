namespace PRM.Models.DTOs.Resources;

public class ResourceListResponse
{
    public List<ResourceListItemDto> Resources { get; set; } = new();

    public int Total { get; set; }

    public int AllocatedCount { get; set; }

    public int BenchCount { get; set; }
}
