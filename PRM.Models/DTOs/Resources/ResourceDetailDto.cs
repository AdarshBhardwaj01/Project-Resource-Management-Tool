namespace PRM.Models.DTOs.Resources;

public class ResourceDetailDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Designation { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public List<ResourceAllocationSummaryDto> ActiveAllocations { get; set; } = new();
}
