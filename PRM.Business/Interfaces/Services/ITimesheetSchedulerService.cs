namespace PRM.Business.Interfaces.Services;

public interface ITimesheetSchedulerService
{
    Task ProcessTimesheetWorkflowAsync(CancellationToken cancellationToken = default);
}
