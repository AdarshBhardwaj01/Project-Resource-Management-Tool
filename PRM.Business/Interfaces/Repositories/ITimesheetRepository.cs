using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface ITimesheetRepository : IRepository<Timesheet>
{
    Task<IReadOnlyList<Timesheet>> GetByEmployeeIdsForWeekAsync(
        IEnumerable<int> employeeIds,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task<Timesheet?> GetByEmployeeIdForWeekAsync(
        int employeeId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task<Timesheet?> GetByEmployeeIdForWeekForUpdateAsync(
        int employeeId,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Timesheet>> GetHistoryByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<Timesheet?> GetByIdForEmployeeAsync(
        int timesheetId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
