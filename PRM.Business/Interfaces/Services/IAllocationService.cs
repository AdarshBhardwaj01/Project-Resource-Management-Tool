using PRM.Models.DTOs.Allocations;

namespace PRM.Business.Interfaces.Services;

public interface IAllocationService
{
    Task<AllocationListResponse> GetAllAllocationsAsync(
        int? employeeId,
        int? projectId,
        string? status,
        CancellationToken cancellationToken = default);
}
