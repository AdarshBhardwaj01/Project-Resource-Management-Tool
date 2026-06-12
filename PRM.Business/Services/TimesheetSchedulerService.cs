using Microsoft.Extensions.Logging;
using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Helpers;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class TimesheetSchedulerService : ITimesheetSchedulerService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<TimesheetSchedulerService> _logger;

    public TimesheetSchedulerService(
        IResourceRepository resourceRepository,
        IAllocationRepository allocationRepository,
        ITimesheetRepository timesheetRepository,
        IEmailNotificationService emailNotificationService,
        ILogger<TimesheetSchedulerService> logger)
    {
        _resourceRepository = resourceRepository;
        _allocationRepository = allocationRepository;
        _timesheetRepository = timesheetRepository;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    public async Task ProcessTimesheetWorkflowAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var currentWeekStart = WeekHelper.GetWeekStartDate(today);
        var previousWeekStart = currentWeekStart.AddDays(-7);
        var workingDaysAfterDeadline = TimesheetWorkflowHelper.GetWorkingDaysAfterDeadline(
            previousWeekStart,
            today);
        if (workingDaysAfterDeadline <= 0)
        {
            return;
        }
        var weekEnd = WeekHelper.GetWeekWorkingEndDate(previousWeekStart);
        var resources = await _resourceRepository.GetAllActiveResourcesWithSkillsAsync(cancellationToken);
        var hasChanges = false;
        foreach (var resource in resources)
        {
            if (!resource.User.IsActive)
            {
                continue;
            }
            var allocations = await _allocationRepository.GetByUserIdForPeriodAsync(
                resource.UserId,
                previousWeekStart,
                weekEnd,
                cancellationToken);
            if (allocations.Count == 0)
            {
                continue;
            }
            var timesheet = await _timesheetRepository.GetByUserIdForWeekForUpdateAsync(
                resource.UserId,
                previousWeekStart,
                cancellationToken);
            if (timesheet?.Status == TimesheetStatus.Submitted)
            {
                continue;
            }
            timesheet ??= await CreatePendingTimesheetAsync(resource.UserId, previousWeekStart, cancellationToken);
            if (timesheet.Status == TimesheetStatus.Submitted)
            {
                continue;
            }
            if (timesheet.IsUnlockedByManager)
            {
                continue;
            }
            if (TimesheetWorkflowHelper.IsFrozen(timesheet))
            {
                continue;
            }
            var employeeEmail = resource.User.Email;
            var employeeName = resource.User.FullName;
            var managerEmail = resource.Manager?.Email ?? string.Empty;
            var managerName = resource.Manager?.FullName ?? "Manager";
            if (workingDaysAfterDeadline == 1 && timesheet.ReminderCount < 1)
            {
                timesheet.ReminderCount = 1;
                timesheet.Status = TimesheetStatus.Pending;
                await _emailNotificationService.SendTimesheetReminderAsync(
                    employeeEmail,
                    employeeName,
                    managerEmail,
                    managerName,
                    previousWeekStart,
                    reminderNumber: 1,
                    cancellationToken);
                hasChanges = true;
                _logger.LogInformation(
                    "Sent timesheet reminder 1 for user {UserId}, week {WeekStart:dd-MMM-yyyy}.",
                    resource.UserId,
                    previousWeekStart);
            }
            else if (workingDaysAfterDeadline == 2 && timesheet.ReminderCount < 2)
            {
                timesheet.ReminderCount = 2;
                timesheet.Status = TimesheetStatus.Pending;
                await _emailNotificationService.SendTimesheetReminderAsync(
                    employeeEmail,
                    employeeName,
                    managerEmail,
                    managerName,
                    previousWeekStart,
                    reminderNumber: 2,
                    cancellationToken);
                hasChanges = true;
                _logger.LogInformation(
                    "Sent timesheet reminder 2 for user {UserId}, week {WeekStart:dd-MMM-yyyy}.",
                    resource.UserId,
                    previousWeekStart);
            }
            else if (workingDaysAfterDeadline >= 3
                     && !timesheet.IsFrozen
                     && !timesheet.IsUnlockedByManager)
            {
                timesheet.IsFrozen = true;
                timesheet.IsUnlockedByManager = false;
                timesheet.Status = TimesheetStatus.Frozen;
                await _emailNotificationService.SendTimesheetFrozenAsync(
                    employeeEmail,
                    employeeName,
                    managerEmail,
                    managerName,
                    previousWeekStart,
                    cancellationToken);
                hasChanges = true;
                _logger.LogInformation(
                    "Frozen timesheet for user {UserId}, week {WeekStart:dd-MMM-yyyy}.",
                    resource.UserId,
                    previousWeekStart);
            }
        }
        if (hasChanges)
        {
            await _timesheetRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Timesheet> CreatePendingTimesheetAsync(
        int userId,
        DateTime weekStartDate,
        CancellationToken cancellationToken)
    {
        var timesheet = new Timesheet
        {
            UserId = userId,
            WeekStartDate = weekStartDate.Date,
            Status = TimesheetStatus.Pending,
            TotalHours = 0,
            IsFrozen = false,
            IsUnlockedByManager = false,
            ReminderCount = 0
        };
        await _timesheetRepository.AddAsync(timesheet, cancellationToken);
        await _timesheetRepository.SaveChangesAsync(cancellationToken);
        return timesheet;
    }
}
