using PRM.Common.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Allocations;
using PRM.Models.Entities;

namespace PRM.Business.Services;

public class AllocationService : IAllocationService
{
    private readonly IAllocationRepository _allocationRepository;

    public AllocationService(IAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public async Task<AllocationListResponse> GetAllAllocationsAsync(
        int? employeeId,
        int? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();

            if (normalizedStatus is not ("ACTIVE" or "EXPIRED"))
            {
                throw new BusinessValidationException("Invalid status filter. Use ACTIVE or EXPIRED.");
            }

            status = normalizedStatus;
        }

        var allocations = await _allocationRepository.GetAllAsync(
            employeeId,
            projectId,
            status,
            cancellationToken);

        var allocationDtos = allocations
            .Select(MapToListItem)
            .ToList();

        return new AllocationListResponse
        {
            Allocations = allocationDtos,
            Total = allocationDtos.Count,
            ActiveCount = allocationDtos.Count(allocation => allocation.Status == "ACTIVE"),
            ExpiredCount = allocationDtos.Count(allocation => allocation.Status == "EXPIRED")
        };
    }

    private static AllocationListItemDto MapToListItem(Allocation allocation)
    {
        var today = DateTime.UtcNow.Date;

        return new AllocationListItemDto
        {
            Id = allocation.Id,
            EmployeeName = allocation.Employee.FullName,
            ProjectName = allocation.Project.Name,
            Role = allocation.Employee.Designation,
            UtilisationPercent = allocation.UtilisationPercent,
            FromDate = allocation.FromDate.ToString("dd-MMM-yy"),
            ToDate = allocation.ToDate.ToString("dd-MMM-yy"),
            Status = AllocationDateRules.IsScheduled(allocation.ToDate, today)
                ? "ACTIVE"
                : "EXPIRED"
        };
    }
}
