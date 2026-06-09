using Microsoft.EntityFrameworkCore;
using PRM.Common.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.DataAccess.Repositories;

public class AllocationRepository : GenericRepository<Allocation>, IAllocationRepository
{
    public AllocationRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Allocation>> GetAllAsync(
        int? employeeId,
        int? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = DbSet
            .Include(allocation => allocation.Employee)
                .ThenInclude(employee => employee.User)
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.Employee.IsActive &&
                allocation.Employee.User.Role == UserRole.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(allocation => allocation.EmployeeId == employeeId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(allocation => allocation.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();

            query = normalizedStatus switch
            {
                "ACTIVE" => query.Where(allocation => allocation.ToDate.Date > today),
                "EXPIRED" => query.Where(allocation => allocation.ToDate.Date <= today),
                _ => query
            };
        }

        return await query
            .OrderBy(allocation => allocation.Employee.FullName)
            .ThenBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveUtilisationTotalAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await DbSet
            .Where(allocation =>
                allocation.EmployeeId == employeeId &&
                allocation.FromDate.Date <= today &&
                allocation.ToDate.Date > today)
            .SumAsync(allocation => allocation.UtilisationPercent, cancellationToken);
    }

    public async Task<int> GetOverlappingUtilisationTotalAsync(
        int employeeId,
        DateTime fromDate,
        DateTime toDate,
        int? excludeAllocationId = null,
        int? excludeProjectId = null,
        CancellationToken cancellationToken = default)
    {
        var periodStart = fromDate.Date;
        var periodEnd = toDate.Date;
        var today = DateTime.UtcNow.Date;

        var query = DbSet.Where(allocation => allocation.EmployeeId == employeeId);

        if (periodStart > today)
        {
            query = query.Where(allocation =>
                allocation.FromDate.Date <= today &&
                allocation.ToDate.Date > today &&
                allocation.FromDate.Date <= periodEnd &&
                allocation.ToDate.Date >= periodStart);
        }
        else
        {
            query = query.Where(allocation =>
                allocation.FromDate.Date <= periodEnd &&
                allocation.ToDate.Date >= periodStart &&
                allocation.FromDate.Date <= periodStart &&
                allocation.ToDate.Date > periodStart);
        }

        if (excludeAllocationId.HasValue)
        {
            query = query.Where(allocation => allocation.Id != excludeAllocationId.Value);
        }

        if (excludeProjectId.HasValue)
        {
            query = query.Where(allocation => allocation.ProjectId != excludeProjectId.Value);
        }

        return await query.SumAsync(allocation => allocation.UtilisationPercent, cancellationToken);
    }

    public async Task<Allocation?> GetOverlappingAllocationOnProjectAsync(
        int employeeId,
        int projectId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var periodStart = fromDate.Date;
        var periodEnd = toDate.Date;
        var today = DateTime.UtcNow.Date;

        return await DbSet
            .Include(allocation => allocation.Project)
            .FirstOrDefaultAsync(
                allocation =>
                    allocation.EmployeeId == employeeId &&
                    allocation.ProjectId == projectId &&
                    allocation.FromDate.Date <= periodEnd &&
                    allocation.ToDate.Date >= periodStart &&
                    allocation.ToDate.Date > today,
                cancellationToken);
    }

    public async Task<bool> HasActiveAllocationsAsync(
        int employeeId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var query = DbSet.Where(allocation =>
            allocation.EmployeeId == employeeId &&
            allocation.ToDate.Date > today);

        if (excludeAllocationId.HasValue)
        {
            query = query.Where(allocation => allocation.Id != excludeAllocationId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetActiveByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await DbSet
            .Include(allocation => allocation.Employee)
                .ThenInclude(employee => employee.User)
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.ProjectId == projectId &&
                allocation.ToDate.Date > today &&
                allocation.Employee.IsActive &&
                allocation.Employee.User.Role == UserRole.Employee)
            .OrderBy(allocation => allocation.Employee.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Allocation?> GetByIdForUpdateAsync(
        int allocationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(allocation => allocation.Employee)
                .ThenInclude(employee => employee.User)
            .Include(allocation => allocation.Employee)
                .ThenInclude(employee => employee.Allocations)
            .Include(allocation => allocation.Project)
            .FirstOrDefaultAsync(allocation => allocation.Id == allocationId, cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetByManagerIdForPeriodAsync(
        int managerUserId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(allocation => allocation.Employee)
                .ThenInclude(employee => employee.User)
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.Project.ManagerId == managerUserId &&
                allocation.FromDate.Date <= periodEnd.Date &&
                allocation.ToDate.Date >= periodStart.Date &&
                allocation.Employee.IsActive &&
                allocation.Employee.User.Role == UserRole.Employee)
            .OrderBy(allocation => allocation.Employee.FullName)
            .ThenBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetByEmployeeIdForPeriodAsync(
        int employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.EmployeeId == employeeId &&
                allocation.FromDate.Date <= periodEnd.Date &&
                allocation.ToDate.Date >= periodStart.Date)
            .OrderBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetScheduledByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await DbSet
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.EmployeeId == employeeId &&
                allocation.ToDate.Date > today)
            .OrderBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
