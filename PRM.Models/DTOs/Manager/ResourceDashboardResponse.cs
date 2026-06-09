namespace PRM.Models.DTOs.Manager;

public class ResourceDashboardResponse
{
    public List<ResourceDashboardBenchItemDto> BenchEmployees { get; set; } = new();

    public List<ResourceDashboardActiveItemDto> ActiveEmployees { get; set; } = new();

    public int BenchCount { get; set; }

    public int OverUtilisedCount { get; set; }

    public int PartialCount { get; set; }
}
