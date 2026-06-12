namespace PRM.Business.Interfaces.Services;

public interface IEmailNotificationService
{
    Task SendTestEmailAsync(
        string toEmail,
        CancellationToken cancellationToken = default);

    Task SendTimesheetReminderAsync(
        string employeeEmail,
        string employeeName,
        string managerEmail,
        string managerName,
        DateTime weekStartDate,
        int reminderNumber,
        CancellationToken cancellationToken = default);

    Task SendTimesheetFrozenAsync(
        string employeeEmail,
        string employeeName,
        string managerEmail,
        string managerName,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default);

    Task SendProjectAtRiskAsync(
        string managerEmail,
        string managerName,
        string projectName,
        string healthStatus,
        string summary,
        CancellationToken cancellationToken = default);
}
