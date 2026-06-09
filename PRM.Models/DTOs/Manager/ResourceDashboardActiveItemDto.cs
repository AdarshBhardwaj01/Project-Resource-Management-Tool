namespace PRM.Models.DTOs.Manager;

public class ResourceDashboardActiveItemDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public int AllocatedPercent { get; set; }

    public string Availability { get; set; } = string.Empty;
}
