using Microsoft.EntityFrameworkCore;
using PRM.Common.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.DataAccess.Repositories;

public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(employee => employee.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            employee => employee.UserId == userId && employee.IsActive,
            cancellationToken);
    }

    public async Task<bool> RestoreInactiveByUserIdAsync(
        int userId,
        string fullName,
        string email,
        string department,
        string designation,
        CancellationToken cancellationToken = default)
    {
        var rowsAffected = await DbSet
            .Where(employee => employee.UserId == userId && !employee.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(employee => employee.IsActive, true)
                    .SetProperty(employee => employee.Status, EmployeeStatus.Bench)
                    .SetProperty(employee => employee.UtilisationPercent, 0)
                    .SetProperty(employee => employee.FullName, fullName.Trim())
                    .SetProperty(employee => employee.Email, email.Trim())
                    .SetProperty(employee => employee.Department, department.Trim())
                    .SetProperty(employee => employee.Designation, designation.Trim()),
                cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> ReactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var employee = await DbSet.FirstOrDefaultAsync(
            employee => employee.UserId == userId,
            cancellationToken);

        if (employee is null || employee.IsActive)
        {
            return false;
        }

        employee.IsActive = true;
        employee.Status = EmployeeStatus.Bench;
        return true;
    }

    public async Task<bool> DeactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var employee = await DbSet.FirstOrDefaultAsync(
            employee => employee.UserId == userId && employee.IsActive,
            cancellationToken);

        if (employee is null)
        {
            return false;
        }

        employee.IsActive = false;
        employee.Status = EmployeeStatus.Bench;
        return true;
    }

    public async Task<Employee?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(employee => employee.User)
            .Include(employee => employee.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(employee => employee.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByIdForSchedulerUpdateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var trackedEntry = Context.ChangeTracker.Entries<Employee>()
            .FirstOrDefault(entry => entry.Entity.Id == id);

        if (trackedEntry is not null)
        {
            trackedEntry.State = EntityState.Detached;
        }

        return await DbSet
            .Include(employee => employee.Allocations)
            .FirstOrDefaultAsync(
                employee => employee.Id == id && employee.IsActive,
                cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(employee => employee.UserId == userId, cancellationToken);
    }

    public async Task<Employee?> GetActiveEmployeeByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(employee => employee.User)
            .FirstOrDefaultAsync(
                employee => employee.UserId == userId && employee.IsActive,
                cancellationToken);
    }

    public async Task<bool> IsAssignedToManagerAsync(
        int employeeId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            employee =>
                employee.Id == employeeId &&
                employee.IsActive &&
                employee.ManagerId == managerUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(
        EmployeeStatus? status,
        string? department,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(employee => employee.IsActive)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(employee => employee.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(employee => employee.Department == department);
        }

        return await query
            .OrderBy(employee => employee.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await Context.Allocations
            .Include(allocation => allocation.Project)
            .Where(allocation =>
                allocation.EmployeeId == employeeId &&
                allocation.FromDate.Date <= today &&
                allocation.ToDate.Date > today)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesWithSkillsForDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(employee => employee.User)
            .Include(employee => employee.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(employee => employee.Allocations)
            .Include(employee => employee.Timesheets)
                .ThenInclude(timesheet => timesheet.Entries)
            .Where(employee =>
                employee.IsActive &&
                employee.User.Role == UserRole.Employee &&
                employee.ManagerId == managerUserId)
            .OrderBy(employee => employee.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetTeamEmployeesWithAllocationsAsync(
        int managerUserId,
        DateTime weekStart,
        DateTime weekEnd,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(employee => employee.User)
            .Include(employee => employee.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .Where(employee =>
                employee.IsActive &&
                employee.User.Role == UserRole.Employee &&
                employee.ManagerId == managerUserId)
            .OrderBy(employee => employee.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Employee?> GetEmployeeForDrillDownAsync(
        int employeeId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(employee => employee.User)
            .Include(employee => employee.Skills)
                .ThenInclude(skill => skill.Skill)
            .Include(employee => employee.Allocations)
                .ThenInclude(allocation => allocation.Project)
            .Include(employee => employee.Timesheets)
                .ThenInclude(timesheet => timesheet.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee =>
                    employee.Id == employeeId &&
                    employee.IsActive &&
                    employee.User.Role == UserRole.Employee &&
                    employee.ManagerId == managerUserId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetAllActiveWithAllocationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(employee => employee.Allocations)
            .Where(employee => employee.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(employee => employee.IsActive && employee.User.Role == UserRole.Employee)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);
    }
}
