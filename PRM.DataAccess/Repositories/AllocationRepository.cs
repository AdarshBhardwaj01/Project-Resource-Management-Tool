using Microsoft.EntityFrameworkCore;
using PRM.Common.Constants;
using PRM.Common.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;

namespace PRM.DataAccess.Repositories;

public class AllocationRepository : GenericRepository<Allocation>, IAllocationRepository
{
    public AllocationRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Allocation>> GetAllAsync(
        int? userId,
        int? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = DbSet
            .Include(allocation => allocation.Resource)
                .ThenInclude(resource => resource.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.Resource.User.IsActive &&
                allocation.Resource.User.UserRoles.Any(userRole =>
                    userRole.Role.RoleName == RoleNames.Employee ||
                    userRole.Role.RoleName == RoleNames.Manager))
            .AsQueryable();
        if (userId.HasValue)
        {
            query = query.Where(allocation => allocation.UserId == userId.Value);
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
            .OrderBy(allocation => allocation.Resource.User.FullName)
            .ThenBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveUtilisationTotalAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet
            .Where(allocation =>
                allocation.UserId == userId &&
                allocation.FromDate.Date <= today &&
                allocation.ToDate.Date > today)
            .SumAsync(allocation => allocation.UtilisationPercent, cancellationToken);
    }

    public async Task<int> GetOverlappingUtilisationTotalAsync(
        int userId,
        DateTime fromDate,
        DateTime toDate,
        int? excludeAllocationId = null,
        int? excludeProjectId = null,
        CancellationToken cancellationToken = default)
    {
        var periodStart = fromDate.Date;
        var periodEnd = toDate.Date;
        var today = DateTime.UtcNow.Date;
        var query = DbSet.Where(allocation => allocation.UserId == userId);
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
        int userId,
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
                    allocation.UserId == userId &&
                    allocation.ProjectId == projectId &&
                    allocation.FromDate.Date <= periodEnd &&
                    allocation.ToDate.Date >= periodStart &&
                    allocation.ToDate.Date > today,
                cancellationToken);
    }

    public async Task<bool> HasActiveAllocationsAsync(
        int userId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = DbSet.Where(allocation =>
            allocation.UserId == userId &&
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
            .Include(allocation => allocation.Resource)
                .ThenInclude(resource => resource.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.ProjectId == projectId &&
                allocation.ToDate.Date > today &&
                allocation.Resource.User.IsActive &&
                allocation.Resource.User.UserRoles.Any(userRole =>
                    userRole.Role.RoleName == RoleNames.Employee ||
                    userRole.Role.RoleName == RoleNames.Manager))
            .OrderBy(allocation => allocation.Resource.User.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Allocation?> GetByIdForUpdateAsync(
        int allocationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(allocation => allocation.Resource)
                .ThenInclude(resource => resource.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
            .Include(allocation => allocation.Resource)
                .ThenInclude(resource => resource.Allocations)
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
            .Include(allocation => allocation.Resource)
                .ThenInclude(resource => resource.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.Project.ManagerId == managerUserId &&
                allocation.FromDate.Date <= periodEnd.Date &&
                allocation.ToDate.Date >= periodStart.Date &&
                allocation.Resource.User.IsActive &&
                allocation.Resource.User.UserRoles.Any(userRole =>
                    userRole.Role.RoleName == RoleNames.Employee ||
                    userRole.Role.RoleName == RoleNames.Manager))
            .OrderBy(allocation => allocation.Resource.User.FullName)
            .ThenBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetByUserIdForPeriodAsync(
        int userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.UserId == userId &&
                allocation.FromDate.Date <= periodEnd.Date &&
                allocation.ToDate.Date >= periodStart.Date)
            .OrderBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetScheduledByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.UserId == userId &&
                allocation.ToDate.Date > today)
            .OrderBy(allocation => allocation.Project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
