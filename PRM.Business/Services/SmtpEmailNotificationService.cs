using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;

namespace PRM.Business.Services;

public class SmtpEmailNotificationService : IEmailNotificationService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailNotificationService> _logger;

    public SmtpEmailNotificationService(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailNotificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        var subject = "PRM Tool - Test Email";
        var body =
            "This is a test email from PRM Tool.\n\n" +
            "If you received this message, SMTP email is configured correctly.\n\n" +
            "Regards,\nPRM Tool";

        _logger.LogInformation("Sending PRM test email to {RecipientEmail}.", toEmail);

        return SendEmailAsync(
            [toEmail],
            ccAddresses: [],
            subject,
            body,
            cancellationToken);
    }

    public async Task SendTimesheetReminderAsync(
        string employeeEmail,
        string employeeName,
        string managerEmail,
        string managerName,
        DateTime weekStartDate,
        int reminderNumber,
        CancellationToken cancellationToken = default)
    {
        var weekLabel = weekStartDate.ToString("dd-MMM-yyyy");
        var employeeSubject = $"Timesheet Reminder #{reminderNumber} - Week starting {weekLabel}";
        var employeeBody =
            $"Dear {employeeName},\n\n" +
            $"This is reminder #{reminderNumber} to submit your timesheet for the week starting {weekLabel}.\n\n" +
            "Please log in to PRM Tool and submit your timesheet as soon as possible.\n\n" +
            "Regards,\nPRM Tool";

        _logger.LogInformation(
            "Timesheet reminder #{ReminderNumber} for week {WeekStart:dd-MMM-yyyy} | Employee: {EmployeeName} <{EmployeeEmail}>",
            reminderNumber,
            weekStartDate,
            employeeName,
            employeeEmail);

        await SendEmailAsync(
            [employeeEmail],
            ccAddresses: [],
            employeeSubject,
            employeeBody,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(managerEmail))
        {
            return;
        }

        var managerSubject =
            $"Team Timesheet Reminder #{reminderNumber} - {employeeName} - Week starting {weekLabel}";
        var managerBody =
            $"Dear {managerName},\n\n" +
            $"Your team member {employeeName} has not submitted their timesheet for the week starting {weekLabel}.\n\n" +
            $"This is reminder #{reminderNumber} sent to the employee. Please follow up and ensure the timesheet is submitted promptly.\n\n" +
            "If the timesheet remains unsubmitted, it will be frozen and can only be restored by you from " +
            "Manager menu -> Restore Frozen Timesheets.\n\n" +
            "Regards,\nPRM Tool";

        _logger.LogInformation(
            "Timesheet reminder #{ReminderNumber} manager notification for week {WeekStart:dd-MMM-yyyy} | " +
            "Manager: {ManagerName} <{ManagerEmail}> | Employee: {EmployeeName}",
            reminderNumber,
            weekStartDate,
            managerName,
            managerEmail,
            employeeName);

        await SendEmailAsync(
            [managerEmail],
            ccAddresses: [],
            managerSubject,
            managerBody,
            cancellationToken);
    }

    public async Task SendTimesheetFrozenAsync(
        string employeeEmail,
        string employeeName,
        string managerEmail,
        string managerName,
        DateTime weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var weekLabel = weekStartDate.ToString("dd-MMM-yyyy");
        var employeeSubject = $"Timesheet Frozen - Week starting {weekLabel}";
        var employeeBody =
            $"Dear {employeeName},\n\n" +
            $"Your timesheet for the week starting {weekLabel} has been frozen because it was not submitted on time.\n\n" +
            "You cannot submit until your manager restores access from the Restore Frozen Timesheets screen.\n\n" +
            "Regards,\nPRM Tool";

        _logger.LogInformation(
            "Timesheet frozen notification for week {WeekStart:dd-MMM-yyyy} | Employee: {EmployeeName} <{EmployeeEmail}>",
            weekStartDate,
            employeeName,
            employeeEmail);

        await SendEmailAsync(
            [employeeEmail],
            ccAddresses: [],
            employeeSubject,
            employeeBody,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(managerEmail))
        {
            return;
        }

        var managerSubject = $"Team Timesheet Missed - {employeeName} - Week starting {weekLabel}";
        var managerBody =
            $"Dear {managerName},\n\n" +
            $"{employeeName} did not submit their timesheet for the week starting {weekLabel}. " +
            "The timesheet has been frozen and the employee can no longer submit it.\n\n" +
            "To allow the employee to submit for that week, open PRM Tool as Manager and use " +
            "Restore Frozen Timesheets to restore access.\n\n" +
            "Regards,\nPRM Tool";

        _logger.LogInformation(
            "Timesheet frozen manager notification for week {WeekStart:dd-MMM-yyyy} | " +
            "Manager: {ManagerName} <{ManagerEmail}> | Employee: {EmployeeName}",
            weekStartDate,
            managerName,
            managerEmail,
            employeeName);

        await SendEmailAsync(
            [managerEmail],
            ccAddresses: [],
            managerSubject,
            managerBody,
            cancellationToken);
    }

    public Task SendProjectAtRiskAsync(
        string managerEmail,
        string managerName,
        string projectName,
        string healthStatus,
        string summary,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Project AT RISK - {projectName}";
        var body =
            $"Dear {managerName},\n\n" +
            $"Project \"{projectName}\" is now marked as {healthStatus}.\n\n" +
            $"{summary}\n\n" +
            "Please review allocations and project health in PRM Tool.\n\n" +
            "Regards,\nPRM Tool";

        _logger.LogInformation(
            "Project AT RISK notification | Project: {ProjectName} | Manager: {ManagerName} <{ManagerEmail}>",
            projectName,
            managerName,
            managerEmail);

        return SendEmailAsync(
            [managerEmail],
            ccAddresses: [],
            subject,
            body,
            cancellationToken);
    }

    private async Task SendEmailAsync(
        IReadOnlyList<string> toAddresses,
        IReadOnlyList<string> ccAddresses,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var validToAddresses = toAddresses
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (validToAddresses.Count == 0)
        {
            _logger.LogWarning("Email not sent for subject \"{Subject}\" because no recipient address was provided.", subject);
            return;
        }

        if (!_settings.IsConfigured)
        {
            _logger.LogWarning(
                "EmailSettings is not configured. Email not sent. Subject: {Subject} | To: {Recipients}",
                subject,
                string.Join(", ", validToAddresses));
            return;
        }

        try
        {
            var message = BuildMessage(validToAddresses, ccAddresses, subject, body);
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);
            await client.AuthenticateAsync(_settings.Email, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
            _logger.LogInformation(
                "Email sent via SMTP. Subject: {Subject} | To: {Recipients}",
                subject,
                string.Join(", ", validToAddresses));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email via SMTP. Subject: {Subject} | To: {Recipients}",
                subject,
                string.Join(", ", validToAddresses));
        }
    }

    private MimeMessage BuildMessage(
        IReadOnlyList<string> toAddresses,
        IReadOnlyList<string> ccAddresses,
        string subject,
        string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.Email));
        foreach (var address in toAddresses)
        {
            message.To.Add(MailboxAddress.Parse(address));
        }
        foreach (var address in ccAddresses.Where(address => !string.IsNullOrWhiteSpace(address)))
        {
            message.Cc.Add(MailboxAddress.Parse(address.Trim()));
        }
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }
}
