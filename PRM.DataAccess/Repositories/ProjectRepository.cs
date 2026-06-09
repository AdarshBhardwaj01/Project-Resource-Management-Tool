using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.DataAccess.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await DbSet.AnyAsync(
            project => project.Name == name.Trim(),
            cancellationToken);
    }

    public async Task<Project?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(project => project.Manager)
            .Include(project => project.Milestones)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(
        ProjectStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(project => project.Manager)
            .Include(project => project.Milestones)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(project => project.Status == status.Value);
        }

        return await query
            .OrderBy(project => project.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByManagerIdAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(project => project.ManagerId == managerUserId)
            .OrderBy(project => project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByManagerIdWithDetailsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(project => project.ManagerId == managerUserId)
            .Include(project => project.Milestones)
            .Include(project => project.Allocations)
                .ThenInclude(allocation => allocation.Employee)
                    .ThenInclude(employee => employee.User)
            .Include(project => project.TimesheetEntries)
                .ThenInclude(entry => entry.Timesheet)
            .OrderBy(project => project.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdForManagerAsync(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            project => project.Id == projectId && project.ManagerId == managerUserId,
            cancellationToken);
    }

    public async Task<Project?> GetByIdForManagerWithDetailsAsync(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(project => project.Milestones)
            .Include(project => project.Allocations)
                .ThenInclude(allocation => allocation.Employee)
                    .ThenInclude(employee => employee.User)
            .Include(project => project.TimesheetEntries)
                .ThenInclude(entry => entry.Timesheet)
                    .ThenInclude(timesheet => timesheet.Employee)
            .FirstOrDefaultAsync(
                project => project.Id == projectId && project.ManagerId == managerUserId,
                cancellationToken);
    }

    public async Task<bool> HasManagedProjectsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            project => project.ManagerId == managerUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllForHealthSchedulerAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(project => project.Milestones)
            .Include(project => project.Allocations)
                .ThenInclude(allocation => allocation.Employee)
                    .ThenInclude(employee => employee.User)
            .Include(project => project.TimesheetEntries)
                .ThenInclude(entry => entry.Timesheet)
            .OrderBy(project => project.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdForHealthSchedulerAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(project => project.Milestones)
            .Include(project => project.Allocations)
                .ThenInclude(allocation => allocation.Employee)
                    .ThenInclude(employee => employee.User)
            .Include(project => project.TimesheetEntries)
                .ThenInclude(entry => entry.Timesheet)
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetManagerUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Select(project => project.ManagerId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
