using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface ITimesheetRepository : IRepository<Timesheet>
{
    Task<IReadOnlyList<Timesheet>> GetByUserIdsForWeekAsync(
        IEnumerable<int> userIds,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByUserIdForWeekAsync(
        int userId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByUserIdForWeekForUpdateAsync(
        int userId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> GetHistoryByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByIdForUserAsync(
        int timesheetId,
        int userId,
        CancellationToken cancellationToken = default);
}
