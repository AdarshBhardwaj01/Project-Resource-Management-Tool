using PRM.Common.Helpers;
using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.EmployeePortal;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class EmployeePortalService : IEmployeePortalService
{
    private readonly IUserRepository _userRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly ISystemConfigRepository _systemConfigRepository;

    public EmployeePortalService(
        IUserRepository userRepository,
        IResourceRepository resourceRepository,
        IAllocationRepository allocationRepository,
        ITimesheetRepository timesheetRepository,
        ISystemConfigRepository systemConfigRepository)
    {
        _userRepository = userRepository;
        _resourceRepository = resourceRepository;
        _allocationRepository = allocationRepository;
        _timesheetRepository = timesheetRepository;
        _systemConfigRepository = systemConfigRepository;
    }

    public async Task<IReadOnlyList<EmployeeAllocationItemDto>> GetMyAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var allocations = await _allocationRepository.GetScheduledByUserIdAsync(
            resource.UserId,
            cancellationToken);
        return allocations
            .Select(allocation => new EmployeeAllocationItemDto
            {
                ProjectName = allocation.Project.Name,
                UtilisationPercent = allocation.UtilisationPercent,
                FromDate = allocation.FromDate.ToString("dd-MMM-yy"),
                ToDate = allocation.ToDate.ToString("dd-MMM-yy"),
                Status = AllocationDateRules.IsScheduled(allocation.ToDate)
                    ? "ACTIVE"
                    : "EXPIRED"
            })
            .ToList();
    }

    public async Task<TimesheetSubmitPreviewResponse> GetTimesheetSubmitPreviewAsync(
        int userId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var weekStart = WeekHelper.GetWeekStartDate(weekStartDate ?? DateTime.UtcNow.Date);
        var weekEnd = WeekHelper.GetWeekEndDate(weekStart);
        var config = await _systemConfigRepository.GetSingletonAsync(cancellationToken)
            ?? throw new BusinessValidationException("System configuration not found.");
        var allocations = await _allocationRepository.GetByUserIdForPeriodAsync(
            resource.UserId,
            weekStart,
            weekEnd,
            cancellationToken);
        var existingTimesheet = await _timesheetRepository.GetByUserIdForWeekAsync(
            resource.UserId,
            weekStart,
            cancellationToken);
        return new TimesheetSubmitPreviewResponse
        {
            EmployeeName = resource.User.FullName,
            WeekStartDate = weekStart.ToString("dd-MM-yyyy"),
            MaxWeeklyHours = config.MaxWeeklyHours,
            AlreadySubmitted = existingTimesheet?.Status == TimesheetStatus.Submitted,
            Projects = allocations
                .Select(allocation => new TimesheetSubmitProjectItemDto
                {
                    ProjectId = allocation.ProjectId,
                    ProjectName = allocation.Project.Name,
                    UtilisationPercent = allocation.UtilisationPercent,
                    ExpectedMaxHours = allocation.UtilisationPercent * config.MaxWeeklyHours / 100
                })
                .ToList()
        };
    }

    public async Task<string> SubmitTimesheetAsync(
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var weekStart = ResolveWeekStartDate(request.WeekStartDate);
        var weekEnd = WeekHelper.GetWeekEndDate(weekStart);
        var config = await _systemConfigRepository.GetSingletonAsync(cancellationToken)
            ?? throw new BusinessValidationException("System configuration not found.");
        var existingTimesheet = await _timesheetRepository.GetByUserIdForWeekForUpdateAsync(
            resource.UserId,
            weekStart,
            cancellationToken);
        if (existingTimesheet?.Status == TimesheetStatus.Submitted)
        {
            throw new BusinessValidationException("Timesheet for this week has already been submitted.");
        }
        var allocations = await _allocationRepository.GetByUserIdForPeriodAsync(
            resource.UserId,
            weekStart,
            weekEnd,
            cancellationToken);
        var allowedProjectIds = allocations.Select(allocation => allocation.ProjectId).ToHashSet();
        var entries = request.Entries
            .Where(entry => entry.Hours > 0)
            .ToList();
        if (entries.Count == 0)
        {
            throw new BusinessValidationException("Enter hours for at least one project.");
        }
        foreach (var entry in entries)
        {
            if (!allowedProjectIds.Contains(entry.ProjectId))
            {
                throw new BusinessValidationException("You can only log hours for projects you are allocated to.");
            }
            if (entry.Hours < 0)
            {
                throw new BusinessValidationException("Hours cannot be negative.");
            }
            var allocation = allocations.First(item => item.ProjectId == entry.ProjectId);
            var expectedMaxHours = allocation.UtilisationPercent * config.MaxWeeklyHours / 100;
            if (entry.Hours > expectedMaxHours)
            {
                throw new BusinessValidationException(
                    $"Hours for {allocation.Project.Name} cannot exceed {expectedMaxHours} based on your allocation.");
            }
        }
        var totalHours = entries.Sum(entry => entry.Hours);
        if (totalHours > config.MaxWeeklyHours)
        {
            throw new BusinessValidationException(
                $"Total hours cannot exceed {config.MaxWeeklyHours} per week.");
        }
        if (existingTimesheet is not null)
        {
            existingTimesheet.Status = TimesheetStatus.Submitted;
            existingTimesheet.TotalHours = (int)totalHours;
            existingTimesheet.Entries.Clear();
            foreach (var entry in entries)
            {
                existingTimesheet.Entries.Add(new TimesheetEntry
                {
                    ProjectId = entry.ProjectId,
                    Hours = entry.Hours,
                    ActivityTags = entry.ActivityTags.Trim()
                });
            }
            await _timesheetRepository.SaveChangesAsync(cancellationToken);
            return "Timesheet submitted successfully. Status: SUBMITTED";
        }
        var timesheet = new Timesheet
        {
            UserId = resource.UserId,
            WeekStartDate = weekStart,
            Status = TimesheetStatus.Submitted,
            TotalHours = (int)totalHours,
            Entries = entries
                .Select(entry => new TimesheetEntry
                {
                    ProjectId = entry.ProjectId,
                    Hours = entry.Hours,
                    ActivityTags = entry.ActivityTags.Trim()
                })
                .ToList()
        };
        await _timesheetRepository.AddAsync(timesheet, cancellationToken);
        await _timesheetRepository.SaveChangesAsync(cancellationToken);
        return "Timesheet submitted successfully. Status: SUBMITTED";
    }

    public async Task<IReadOnlyList<EmployeeTimesheetHistoryItemDto>> GetMyTimesheetsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var timesheets = await _timesheetRepository.GetHistoryByUserIdAsync(resource.UserId, cancellationToken);
        return timesheets
            .Select(timesheet => new EmployeeTimesheetHistoryItemDto
            {
                TimesheetId = timesheet.Id,
                WeekStartDate = timesheet.WeekStartDate.ToString("dd-MM-yyyy"),
                TotalHours = timesheet.TotalHours,
                Status = FormatTimesheetStatus(timesheet.Status)
            })
            .ToList();
    }

    public async Task<EmployeeTimesheetDetailDto> GetTimesheetDetailAsync(
        int userId,
        int timesheetId,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var timesheet = await _timesheetRepository.GetByIdForUserAsync(
            timesheetId,
            resource.UserId,
            cancellationToken);
        if (timesheet is null)
        {
            throw new BusinessValidationException("Timesheet not found.");
        }
        return new EmployeeTimesheetDetailDto
        {
            WeekStartDate = timesheet.WeekStartDate.ToString("dd-MM-yyyy"),
            TotalHours = timesheet.TotalHours,
            Status = FormatTimesheetStatus(timesheet.Status),
            Entries = timesheet.Entries
                .OrderBy(entry => entry.Project.Name)
                .Select(entry => new EmployeeTimesheetEntryDetailDto
                {
                    ProjectName = entry.Project.Name,
                    Hours = (int)entry.Hours,
                    ActivityTags = string.IsNullOrWhiteSpace(entry.ActivityTags)
                        ? "(none)"
                        : entry.ActivityTags
                })
                .ToList()
        };
    }

    private async Task<Resource> GetActiveResourceOrThrowAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("User account not found or inactive.");
        }
        if (!UserRoleHelper.HasRole(user, ApplicationRole.Employee))
        {
            throw new BusinessValidationException("Only employees can perform this action.");
        }
        var resource = await _resourceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (resource is null)
        {
            throw new BusinessValidationException("Resource profile not found or inactive.");
        }
        return resource;
    }

    private static DateTime ResolveWeekStartDate(string? weekStartDate)
    {
        if (string.IsNullOrWhiteSpace(weekStartDate))
        {
            return WeekHelper.GetCurrentWeekStartDate();
        }
        return WeekHelper.GetWeekStartDate(DateValidator.ParseRequired(weekStartDate, "Week start date"));
    }

    private static string FormatTimesheetStatus(TimesheetStatus status)
    {
        return status == TimesheetStatus.Missed ? "MISSED" : "SUBMITTED";
    }
}
