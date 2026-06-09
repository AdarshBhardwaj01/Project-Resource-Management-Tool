namespace PRM.Models.DTOs.Manager;

public class EmployeeDrillDownDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string CurrentStatus { get; set; } = string.Empty;

    public string ProfileSkills { get; set; } = string.Empty;

    public List<EmployeeAllocationDetailDto> ActiveAllocations { get; set; } = new();

    public string RecentActivityTags { get; set; } = string.Empty;
}
