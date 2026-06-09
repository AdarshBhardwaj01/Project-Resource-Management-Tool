namespace PRM.Models.DTOs.Allocations;

public class AllocationListResponse
{
    public List<AllocationListItemDto> Allocations { get; set; } = new();

    public int Total { get; set; }

    public int ActiveCount { get; set; }

    public int ExpiredCount { get; set; }
}
