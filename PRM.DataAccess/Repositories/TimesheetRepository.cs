using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.DataAccess.Repositories;

public class TimesheetRepository : GenericRepository<Timesheet>, ITimesheetRepository
{
    public TimesheetRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Timesheet>> GetByUserIdsForWeekAsync(
        IEnumerable<int> userIds,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.Distinct().ToList();
        if (userIdList.Count == 0)
        {
            return [];
        }
        return await DbSet
            .Include(timesheet => timesheet.Entries)
                .ThenInclude(entry => entry.Project)
            .Where(timesheet =>
                userIdList.Contains(timesheet.UserId) &&
                timesheet.WeekStartDate.Date == weekStartDate.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Timesheet?> GetByUserIdForWeekAsync(
        int userId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Resource)
                .ThenInclude(resource => resource.User)
            .Include(timesheet => timesheet.Entries)
                .ThenInclude(entry => entry.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                timesheet =>
                    timesheet.UserId == userId &&
                    timesheet.WeekStartDate.Date == weekStartDate.Date,
                cancellationToken);
    }

    public async Task<Timesheet?> GetByUserIdForWeekForUpdateAsync(
        int userId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Resource)
                .ThenInclude(resource => resource.User)
            .Include(timesheet => timesheet.Entries)
            .FirstOrDefaultAsync(
                timesheet =>
                    timesheet.UserId == userId &&
                    timesheet.WeekStartDate.Date == weekStartDate.Date,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Timesheet>> GetHistoryByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(timesheet => timesheet.UserId == userId)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Timesheet?> GetByIdForUserAsync(
        int timesheetId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Entries)
                .ThenInclude(entry => entry.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                timesheet => timesheet.Id == timesheetId && timesheet.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Timesheet>> GetFrozenTimesheetsForManagerAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(timesheet => timesheet.Resource)
                .ThenInclude(resource => resource.User)
            .Where(timesheet =>
                timesheet.IsFrozen
                && !timesheet.IsUnlockedByManager
                && timesheet.Status != TimesheetStatus.Submitted
                && timesheet.Resource.ManagerUserId == managerUserId
                && timesheet.Resource.User.IsActive)
            .OrderByDescending(timesheet => timesheet.WeekStartDate)
            .ThenBy(timesheet => timesheet.Resource.User.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
