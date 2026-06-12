using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.Common.Constants;
using PRM.DataAccess.Context;
using PRM.DataAccess.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.DataAccess.Repositories;

public class ResourceRepository : GenericRepository<Resource>, IResourceRepository
{
    public ResourceRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(resource => resource.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            resource => resource.UserId == userId && resource.User.IsActive,
            cancellationToken);
    }

    public async Task<bool> RestoreInactiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await DbSet
            .Where(resource => resource.UserId == userId && !resource.User.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(resource => resource.Status, ResourceStatus.Bench)
                    .SetProperty(resource => resource.UtilisationPercent, 0),
                cancellationToken);
        if (rowsAffected > 0)
        {
            await Context.Users
                .Where(user => user.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(user => user.IsActive, true),
                    cancellationToken);
        }
        return rowsAffected > 0;
    }

    public async Task<bool> ReactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var resource = await DbSet
            .Include(resource => resource.User)
            .FirstOrDefaultAsync(resource => resource.UserId == userId, cancellationToken);
        if (resource is null || resource.User.IsActive)
        {
            return false;
        }
        resource.User.IsActive = true;
        resource.Status = ResourceStatus.Bench;
        return true;
    }

    public async Task<bool> DeactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var resource = await DbSet
            .Include(resource => resource.User)
            .FirstOrDefaultAsync(
                resource => resource.UserId == userId && resource.User.IsActive,
                cancellationToken);
        if (resource is null)
        {
            return false;
        }
        resource.User.IsActive = false;
        resource.Status = ResourceStatus.Bench;
        return true;
    }

    public async Task<Resource?> GetByUserIdWithDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
                .ThenInclude(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(resource => resource.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(resource => resource.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .FirstOrDefaultAsync(resource => resource.UserId == userId, cancellationToken);
    }

    public async Task<Resource?> GetByUserIdForSchedulerUpdateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var trackedEntry = Context.ChangeTracker.Entries<Resource>()
            .FirstOrDefault(entry => entry.Entity.UserId == userId);
        if (trackedEntry is not null)
        {
            trackedEntry.State = EntityState.Detached;
        }
        return await DbSet
            .Include(resource => resource.User)
            .Include(resource => resource.Allocations)
            .FirstOrDefaultAsync(
                resource => resource.UserId == userId && resource.User.IsActive,
                cancellationToken);
    }

    public async Task<Resource?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(resource => resource.UserId == userId, cancellationToken);
    }

    public async Task<Resource?> GetActiveResourceByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
                .ThenInclude(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(
                resource => resource.UserId == userId && resource.User.IsActive,
                cancellationToken);
    }

    public async Task<bool> IsAssignedToManagerAsync(
        int userId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            resource =>
                resource.UserId == userId &&
                resource.User.IsActive &&
                resource.ManagerUserId == managerUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetAllAsync(
        ResourceStatus? status,
        string? department,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(resource => resource.User)
            .Where(resource => resource.User.IsActive)
            .AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(resource => resource.Status == status.Value);
        }
        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(resource => resource.User.Department == department);
        }
        return await query
            .OrderBy(resource => resource.UserId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await Context.Allocations
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.UserId == userId &&
                allocation.FromDate.Date <= today &&
                allocation.ToDate.Date > today)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetResourcesWithSkillsForDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
                .ThenInclude(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(resource => resource.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(resource => resource.Allocations)
            .Include(resource => resource.Timesheets)
                .ThenInclude(timesheet => timesheet.Entries)
            .Where(resource =>
                resource.User.IsActive &&
                resource.User.UserRoles.Any(userRole => userRole.Role.RoleName == RoleNames.Employee) &&
                resource.ManagerUserId == managerUserId)
            .OrderBy(resource => resource.User.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetAllActiveResourcesWithSkillsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
                .ThenInclude(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(resource => resource.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(resource => resource.Allocations)
            .Where(resource =>
                resource.User.IsActive &&
                resource.User.UserRoles.Any(userRole =>
                    userRole.Role.RoleName == RoleNames.Employee ||
                    userRole.Role.RoleName == RoleNames.Manager))
            .OrderBy(resource => resource.User.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetTeamResourcesWithAllocationsAsync(
        int managerUserId,
        DateTime weekStart,
        DateTime weekEnd,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
                .ThenInclude(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(resource => resource.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .Where(resource =>
                resource.User.IsActive &&
                resource.User.UserRoles.Any(userRole => userRole.Role.RoleName == RoleNames.Employee) &&
                resource.ManagerUserId == managerUserId)
            .OrderBy(resource => resource.User.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Resource?> GetResourceForDrillDownAsync(
        int userId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
                .ThenInclude(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .Include(resource => resource.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(resource => resource.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .Include(resource => resource.Timesheets)
                .ThenInclude(timesheet => timesheet.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                resource =>
                    resource.UserId == userId &&
                    resource.User.IsActive &&
                    resource.User.UserRoles.Any(userRole => userRole.Role.RoleName == RoleNames.Employee) &&
                    resource.ManagerUserId == managerUserId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetAllActiveWithAllocationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(resource => resource.User)
            .Include(resource => resource.Allocations)
            .Where(resource => resource.User.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetActiveResourceUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(resource =>
                resource.User.IsActive &&
                resource.User.UserRoles.Any(userRole => userRole.Role.RoleName == RoleNames.Employee))
            .Select(resource => resource.UserId)
            .ToListAsync(cancellationToken);
    }
}
