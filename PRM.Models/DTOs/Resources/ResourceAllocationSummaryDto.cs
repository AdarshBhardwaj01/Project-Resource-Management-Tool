namespace PRM.Models.DTOs.Resources;

public class ResourceAllocationSummaryDto
{
    public string ProjectName { get; set; } = string.Empty;

    public int UtilisationPercent { get; set; }

    public string ToDate { get; set; } = string.Empty;
}
