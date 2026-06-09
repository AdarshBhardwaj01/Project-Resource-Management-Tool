using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;

namespace PRM.DataAccess.Repositories;

public class TimesheetRepository : GenericRepository<Timesheet>, ITimesheetRepository
{
    public TimesheetRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Timesheet>> GetByEmployeeIdsForWeekAsync(
        IEnumerable<int> employeeIds,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var employeeIdList = employeeIds.Distinct().ToList();

        if (employeeIdList.Count == 0)
        {
            return [];
        }

        return await DbSet
            .Include(timesheet => timesheet.Entries)
                .ThenInclude(entry => entry.Project)
            .Where(timesheet =>
                employeeIdList.Contains(timesheet.EmployeeId) &&
                timesheet.WeekStartDate.Date == weekStartDate.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Timesheet?> GetByEmployeeIdForWeekAsync(
        int employeeId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Employee)
                .ThenInclude(employee => employee.User)
            .Include(timesheet => timesheet.Entries)
                .ThenInclude(entry => entry.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                timesheet =>
                    timesheet.EmployeeId == employeeId &&
                    timesheet.WeekStartDate.Date == weekStartDate.Date,
                cancellationToken);
    }

    public async Task<Timesheet?> GetByEmployeeIdForWeekForUpdateAsync(
        int employeeId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Entries)
            .FirstOrDefaultAsync(
                timesheet =>
                    timesheet.EmployeeId == employeeId &&
                    timesheet.WeekStartDate.Date == weekStartDate.Date,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Timesheet>> GetHistoryByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(timesheet => timesheet.EmployeeId == employeeId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Timesheet?> GetByIdForEmployeeAsync(
        int timesheetId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Entries)
                .ThenInclude(entry => entry.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                timesheet => timesheet.Id == timesheetId && timesheet.EmployeeId == employeeId,
                cancellationToken);
    }
}
