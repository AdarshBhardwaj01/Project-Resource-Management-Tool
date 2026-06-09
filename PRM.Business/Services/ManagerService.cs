using PRM.Common.Constants;
using PRM.Common.Helpers;
using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Ai;
using PRM.Models.DTOs.Manager;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class ManagerService : IManagerService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeStatusSchedulerService _employeeStatusSchedulerService;
    private readonly IPrmSchedulerService _prmSchedulerService;
    private readonly IAiService _aiService;

    public ManagerService(
        IEmployeeRepository employeeRepository,
        IProjectRepository projectRepository,
        IAllocationRepository allocationRepository,
        ITimesheetRepository timesheetRepository,
        IUserRepository userRepository,
        IEmployeeStatusSchedulerService employeeStatusSchedulerService,
        IPrmSchedulerService prmSchedulerService,
        IAiService aiService)
    {
        _employeeRepository = employeeRepository;
        _projectRepository = projectRepository;
        _allocationRepository = allocationRepository;
        _timesheetRepository = timesheetRepository;
        _userRepository = userRepository;
        _employeeStatusSchedulerService = employeeStatusSchedulerService;
        _prmSchedulerService = prmSchedulerService;
        _aiService = aiService;
    }

    public async Task<ResourceDashboardResponse> GetResourceDashboardAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);
        await _prmSchedulerService.RecomputeAllEmployeesAsync(cancellationToken);

        var employees = await _employeeRepository.GetEmployeesWithSkillsForDashboardAsync(
            managerUserId,
            cancellationToken);
        var managerUserIds = (await _projectRepository.GetManagerUserIdsAsync(cancellationToken)).ToHashSet();
        var today = DateTime.UtcNow.Date;
        var benchEmployees = new List<ResourceDashboardBenchItemDto>();
        var activeEmployees = new List<ResourceDashboardActiveItemDto>();
        var overUtilisedCount = 0;
        var partialCount = 0;

        foreach (var employee in employees)
        {
            var usedPercent = EmployeeSchedulerHelper.ComputeUtilisationPercent(employee, today);
            var status = EmployeeSchedulerHelper.ComputeStatus(employee, today, managerUserIds);
            var skills = FormatSkills(employee.Skills);

            if (usedPercent > 100)
            {
                overUtilisedCount++;
            }
            else if (usedPercent > 0 && usedPercent < 100)
            {
                partialCount++;
            }

            if (status == EmployeeStatus.Bench)
            {
                benchEmployees.Add(new ResourceDashboardBenchItemDto
                {
                    Id = employee.Id,
                    FullName = employee.FullName,
                    Department = employee.Department,
                    Skills = skills
                });

                continue;
            }

            var availablePercent = 100 - usedPercent;

            activeEmployees.Add(new ResourceDashboardActiveItemDto
            {
                Id = employee.Id,
                FullName = employee.FullName,
                Department = employee.Department,
                Skills = skills,
                AllocatedPercent = usedPercent,
                Availability = FormatAvailability(usedPercent, availablePercent)
            });
        }

        return new ResourceDashboardResponse
        {
            BenchEmployees = benchEmployees,
            ActiveEmployees = activeEmployees,
            BenchCount = benchEmployees.Count,
            OverUtilisedCount = overUtilisedCount,
            PartialCount = partialCount
        };
    }

    public async Task<IReadOnlyList<ManagerProjectItemDto>> GetMyProjectsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var projects = await _projectRepository.GetByManagerIdWithDetailsAsync(managerUserId, cancellationToken);

        return projects
            .Select((project, index) => new ManagerProjectItemDto
            {
                Id = project.Id,
                RowNumber = index + 1,
                Name = project.Name,
                Status = FormatProjectStatus(project.Status),
                StartDate = project.StartDate.ToString("dd-MMM-yy"),
                EndDate = project.EndDate.ToString("dd-MMM-yy"),
                HealthStatus = FormatHealthStatus(project.HealthStatus)
            })
            .ToList();
    }

    public async Task<ManagerProjectDetailDto> GetMyProjectDetailAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var project = await _projectRepository.GetByIdForManagerWithDetailsAsync(
            projectId,
            managerUserId,
            cancellationToken);

        if (project is null)
        {
            throw new BusinessValidationException("Project not found or not assigned to you.");
        }

        var today = DateTime.UtcNow.Date;
        var milestones = project.Milestones
            .OrderBy(milestone => milestone.SortOrder)
            .ThenBy(milestone => milestone.DueDate)
            .ToList();

        var allocations = GetScheduledAllocations(project, today);

        return new ManagerProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            HealthStatus = FormatHealthStatus(project.HealthStatus),
            RiskFlags = BuildProjectRiskFlags(project, allocations, today),
            Milestones = milestones
                .Select((milestone, index) => MapMilestone(milestone, index + 1, today))
                .ToList(),
            Allocations = allocations
                .Select(allocation => new ManagerProjectAllocationDto
                {
                    EmployeeName = allocation.Employee.FullName,
                    UtilisationPercent = allocation.UtilisationPercent,
                    FromDate = allocation.FromDate.ToString("dd-MMM-yy"),
                    ToDate = allocation.ToDate.ToString("dd-MMM-yy")
                })
                .ToList()
        };
    }

    public async Task<string> AllocateResourceAsync(
        int managerUserId,
        CreateAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAllocationAsync(managerUserId, request, cancellationToken);

        if (!validation.IsValid)
        {
            throw new BusinessValidationException(
                "Total utilisation across overlapping allocations in the selected period cannot exceed 100%.");
        }

        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        await GetTeamEmployeeOrThrowAsync(managerUserId, request.EmployeeId, cancellationToken);

        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;

        var allocation = new Allocation
        {
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            UtilisationPercent = request.UtilisationPercent,
            FromDate = fromDate,
            ToDate = toDate
        };

        await _allocationRepository.AddAsync(allocation, cancellationToken);
        await _allocationRepository.SaveChangesAsync(cancellationToken);

        await _employeeStatusSchedulerService.RecomputeEmployeeStatusAsync(
            request.EmployeeId,
            cancellationToken: cancellationToken);

        await _prmSchedulerService.RecomputeProjectHealthAsync(
            request.ProjectId,
            cancellationToken);

        return "Resource allocated successfully.";
    }

    public async Task<EmployeeDrillDownDto> GetEmployeeDrillDownAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var employee = await _employeeRepository.GetEmployeeForDrillDownAsync(
            employeeId,
            managerUserId,
            cancellationToken);

        if (employee is null)
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }

        var today = DateTime.UtcNow.Date;
        var scheduledAllocations = employee.Allocations
            .Where(allocation => allocation.ToDate.Date > today)
            .OrderBy(allocation => allocation.Project.Name)
            .ToList();

        var usedPercent = employee.UtilisationPercent;

        return new EmployeeDrillDownDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Department = employee.Department,
            CurrentStatus = FormatCurrentStatus(usedPercent),
            ProfileSkills = FormatSkills(employee.Skills),
            ActiveAllocations = scheduledAllocations
                .Select(allocation => new EmployeeAllocationDetailDto
                {
                    ProjectName = allocation.Project.Name,
                    UtilisationPercent = allocation.UtilisationPercent,
                    FromDate = allocation.FromDate.ToString("dd-MMM-yy"),
                    ToDate = allocation.ToDate.ToString("dd-MMM-yy")
                })
                .ToList(),
            RecentActivityTags = GetRecentActivityTags(employee)
        };
    }

    public async Task<EmployeeUtilisationPreviewDto> GetEmployeeUtilisationPreviewAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetTeamEmployeeOrThrowAsync(managerUserId, employeeId, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var displayUtilisation = EmployeeSchedulerHelper.ComputeUtilisationPercent(employee, today);

        return new EmployeeUtilisationPreviewDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            CurrentUtilisationPercent = displayUtilisation,
            UtilisationNote = FormatUtilisationNote(displayUtilisation)
        };
    }

    public async Task<AllocationValidationDto> ValidateAllocationAsync(
        int managerUserId,
        CreateAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAllocationRequest(request);
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var employee = await GetTeamEmployeeOrThrowAsync(managerUserId, request.EmployeeId, cancellationToken);
        await ValidateProjectForAllocationAsync(request, managerUserId, cancellationToken);

        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;

        var existingOnProject = await _allocationRepository.GetOverlappingAllocationOnProjectAsync(
            request.EmployeeId,
            request.ProjectId,
            fromDate,
            toDate,
            cancellationToken);

        if (existingOnProject is not null)
        {
            throw new BusinessValidationException(
                $"Employee already has a {existingOnProject.UtilisationPercent}% allocation on " +
                $"{existingOnProject.Project.Name} from " +
                $"{existingOnProject.FromDate:dd-MMM-yyyy} to {existingOnProject.ToDate:dd-MMM-yyyy}.");
        }

        var currentUtilisation = await _allocationRepository.GetOverlappingUtilisationTotalAsync(
            request.EmployeeId,
            fromDate,
            toDate,
            excludeProjectId: request.ProjectId,
            cancellationToken: cancellationToken);

        var totalUtilisation = currentUtilisation + request.UtilisationPercent;

        return new AllocationValidationDto
        {
            EmployeeName = employee.FullName,
            CurrentUtilisation = currentUtilisation,
            ProposedUtilisation = request.UtilisationPercent,
            TotalUtilisation = totalUtilisation,
            IsValid = totalUtilisation <= 100
        };
    }

    public async Task<IReadOnlyList<ProjectAllocationListItemDto>> GetProjectActiveAllocationsAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var project = await _projectRepository.GetByIdForManagerAsync(projectId, managerUserId, cancellationToken);

        if (project is null)
        {
            throw new BusinessValidationException("Project not found or not assigned to you.");
        }

        var allocations = await _allocationRepository.GetActiveByProjectIdAsync(projectId, cancellationToken);

        return allocations
            .Select((allocation, index) => new ProjectAllocationListItemDto
            {
                Id = allocation.Id,
                RowNumber = index + 1,
                EmployeeName = allocation.Employee.FullName,
                UtilisationPercent = allocation.UtilisationPercent,
                FromDate = allocation.FromDate.ToString("dd-MMM-yy"),
                ToDate = allocation.ToDate.ToString("dd-MMM-yy")
            })
            .ToList();
    }

    public async Task<string> EndAllocationAsync(
        int managerUserId,
        int allocationId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var allocation = await _allocationRepository.GetByIdForUpdateAsync(allocationId, cancellationToken);

        if (allocation is null)
        {
            throw new BusinessValidationException("Allocation not found.");
        }

        if (allocation.Project.ManagerId != managerUserId)
        {
            throw new BusinessValidationException("You can only end allocations on your own projects.");
        }

        if (allocation.Employee.User.Role != UserRole.Employee)
        {
            throw new BusinessValidationException("Only employee allocations can be ended.");
        }

        var today = DateTime.UtcNow.Date;

        if (!AllocationDateRules.IsScheduled(allocation.ToDate, today))
        {
            throw new BusinessValidationException("Allocation is already ended.");
        }

        allocation.ToDate = today;

        await _employeeStatusSchedulerService.RecomputeEmployeeStatusAsync(
            allocation.EmployeeId,
            allocation.Id,
            cancellationToken);

        await _allocationRepository.SaveChangesAsync(cancellationToken);

        await _prmSchedulerService.RecomputeProjectHealthAsync(
            allocation.ProjectId,
            cancellationToken);

        return
            $"Allocation ended. {allocation.Employee.FullName} freed from {allocation.Project.Name} " +
            $"as of {today:dd-MMM-yyyy}.";
    }

    public async Task<ManagerTeamTimesheetsResponse> GetTeamTimesheetsAsync(
        int managerUserId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var weekStart = WeekHelper.GetWeekStartDate(weekStartDate ?? DateTime.UtcNow.Date);
        var weekEnd = WeekHelper.GetWeekEndDate(weekStart);
        var teamEmployees = await _employeeRepository.GetTeamEmployeesWithAllocationsAsync(
            managerUserId,
            weekStart,
            weekEnd,
            cancellationToken);

        var employeeIds = teamEmployees.Select(employee => employee.Id).ToList();
        var timesheets = await _timesheetRepository.GetByEmployeeIdsForWeekAsync(
            employeeIds,
            weekStart,
            cancellationToken);
        var timesheetByEmployee = timesheets.ToDictionary(timesheet => timesheet.EmployeeId);

        var rows = new List<ManagerTeamTimesheetRowDto>();

        foreach (var employee in teamEmployees)
        {
            timesheetByEmployee.TryGetValue(employee.Id, out var timesheet);

            var weekAllocations = employee.Allocations
                .Where(allocation =>
                    allocation.FromDate.Date <= weekEnd &&
                    allocation.ToDate.Date >= weekStart)
                .OrderBy(allocation => allocation.Project.Name)
                .ToList();

            if (weekAllocations.Count == 0)
            {
                rows.Add(new ManagerTeamTimesheetRowDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    ProjectId = 0,
                    ProjectName = "(no active allocation)",
                    Hours = 0,
                    Status = FormatTimesheetStatus(timesheet)
                });

                continue;
            }

            foreach (var allocation in weekAllocations)
            {
                var entry = timesheet?.Entries.FirstOrDefault(item => item.ProjectId == allocation.ProjectId);

                rows.Add(new ManagerTeamTimesheetRowDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    ProjectId = allocation.ProjectId,
                    ProjectName = allocation.Project.Name,
                    Hours = entry is null ? 0 : (int)entry.Hours,
                    Status = FormatTimesheetStatus(timesheet)
                });
            }
        }

        return new ManagerTeamTimesheetsResponse
        {
            WeekStartDate = weekStart.ToString("dd-MMM-yyyy"),
            Rows = rows
        };
    }

    public async Task<ManagerEmployeeTimesheetDetailDto> GetEmployeeTimesheetDetailAsync(
        int managerUserId,
        int employeeId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var weekStart = WeekHelper.GetWeekStartDate(weekStartDate ?? DateTime.UtcNow.Date);
        var weekEnd = WeekHelper.GetWeekEndDate(weekStart);

        if (!await _employeeRepository.IsAssignedToManagerAsync(employeeId, managerUserId, cancellationToken))
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }

        var employee = await _employeeRepository.GetByIdWithDetailsAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }

        var employeeAllocations = employee.Allocations
            .Where(allocation =>
                allocation.FromDate.Date <= weekEnd &&
                allocation.ToDate.Date >= weekStart)
            .OrderBy(allocation => allocation.Project.Name)
            .ToList();

        if (employeeAllocations.Count == 0)
        {
            throw new BusinessValidationException("Employee has no active allocations for the selected week.");
        }

        var timesheet = await _timesheetRepository.GetByEmployeeIdForWeekAsync(
            employeeId,
            weekStart,
            cancellationToken);
        var employeeName = employee.FullName;
        var entries = employeeAllocations
            .Select(allocation =>
            {
                var entry = timesheet?.Entries.FirstOrDefault(item => item.ProjectId == allocation.ProjectId);

                return new ManagerEmployeeTimesheetEntryDto
                {
                    ProjectName = allocation.Project.Name,
                    Hours = entry is null ? 0 : (int)entry.Hours,
                    ActivityTags = string.IsNullOrWhiteSpace(entry?.ActivityTags)
                        ? "(none)"
                        : entry.ActivityTags
                };
            })
            .ToList();

        return new ManagerEmployeeTimesheetDetailDto
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            WeekStartDate = weekStart.ToString("dd-MMM-yyyy"),
            TotalHours = entries.Sum(entry => entry.Hours),
            Status = FormatTimesheetStatus(timesheet),
            Entries = entries
        };
    }

    public async Task<SkillMatchResponse> GetSkillMatchAsync(
        int managerUserId,
        SkillMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Requirement))
        {
            throw new BusinessValidationException("Project requirement is required.");
        }

        string? projectName = null;

        if (request.ProjectId is > 0)
        {
            var project = await _projectRepository.GetByIdForManagerAsync(
                request.ProjectId.Value,
                managerUserId,
                cancellationToken);

            if (project is null)
            {
                throw new BusinessValidationException("Project not found or not assigned to you.");
            }

            projectName = project.Name;
        }

        await _prmSchedulerService.RecomputeAllEmployeesAsync(cancellationToken);

        var employees = await _employeeRepository.GetEmployeesWithSkillsForDashboardAsync(
            managerUserId,
            cancellationToken);
        var context = await BuildSkillMatchContext(
            request.Requirement,
            projectName,
            employees,
            cancellationToken);

        if (context.Candidates.Count == 0)
        {
            var parsed = SkillMatchHelper.ParseRequirement(request.Requirement);

            return new SkillMatchResponse
            {
                NoMatchReason = SkillMatchHelper.BuildNoMatchReason(parsed)
            };
        }

        return await _aiService.GetSkillMatchAsync(context, cancellationToken);
    }

    public async Task<ProjectRiskSummaryResponse> GetProjectRiskSummaryAsync(
        int managerUserId,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetMyProjectDetailAsync(managerUserId, projectId, cancellationToken);
        var context = BuildRiskSummaryContext(project);

        return new ProjectRiskSummaryResponse
        {
            ProjectName = project.Name,
            Summary = await _aiService.GetRiskSummaryAsync(context, cancellationToken)
        };
    }

    private async Task<AiSkillMatchContext> BuildSkillMatchContext(
        string requirement,
        string? projectName,
        IReadOnlyList<Employee> employees,
        CancellationToken cancellationToken)
    {
        var parsed = SkillMatchHelper.ParseRequirement(requirement);
        var managerUserIds = (await _projectRepository.GetManagerUserIdsAsync(cancellationToken)).ToHashSet();
        var today = DateTime.UtcNow.Date;
        var evaluationDate = parsed.AvailableFromDate ?? today;
        var candidates = new List<AiSkillMatchCandidateDto>();

        var employeesToConsider = parsed.SkillKeywords.Count > 0
            ? employees.Where(SkillMatchHelper.HasAssignedSkills)
            : employees;

        foreach (var employee in employeesToConsider)
        {
            var usedPercent = EmployeeSchedulerHelper.ComputeUtilisationPercent(employee, evaluationDate);
            var status = EmployeeSchedulerHelper.ComputeStatus(employee, evaluationDate, managerUserIds);
            var skills = FormatSkills(employee.Skills);
            var isOnBench = status == EmployeeStatus.Bench;
            var availability = SkillMatchHelper.FormatAvailabilityForDate(
                usedPercent,
                isOnBench,
                parsed.AvailableFromDate);

            var candidate = new AiSkillMatchCandidateDto
            {
                EmployeeId = employee.Id,
                FullName = employee.FullName,
                Department = employee.Department,
                Skills = skills,
                IsOnBench = isOnBench,
                Availability = availability,
                UtilisationPercent = usedPercent,
                MatchedSkills = SkillMatchHelper.FormatMatchedSkills(skills, parsed.SkillKeywords),
                RecentActivity = SkillMatchHelper.FormatRecentActivity(employee, parsed.SkillKeywords)
            };

            if (!SkillMatchHelper.IsEligibleCandidate(candidate, parsed))
            {
                continue;
            }

            candidates.Add(candidate);
        }

        return new AiSkillMatchContext
        {
            Requirement = requirement,
            ProjectName = projectName,
            MinAvailablePercent = parsed.MinAvailablePercent,
            AvailableFromDate = parsed.AvailableFromDate,
            RequireFullAvailability = parsed.RequireFullAvailability,
            Candidates = candidates
        };
    }

    private static AiRiskSummaryContext BuildRiskSummaryContext(ManagerProjectDetailDto project)
    {
        return new AiRiskSummaryContext
        {
            ProjectName = project.Name,
            HealthStatus = project.HealthStatus,
            Milestones = project.Milestones
                .Select(milestone => $"{milestone.Title} ({milestone.DueDate}) - {milestone.Status}")
                .ToList(),
            Allocations = project.Allocations
                .Select(allocation =>
                    $"{allocation.EmployeeName}: {allocation.UtilisationPercent}% " +
                    $"({allocation.FromDate} to {allocation.ToDate})")
                .ToList(),
            RiskFlags = project.RiskFlags
                .Where(flag => !flag.IsPositive)
                .Select(flag => flag.Message)
                .ToList()
        };
    }

    private async Task ValidateManagerUserAsync(int managerUserId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(managerUserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("Manager account not found or inactive.");
        }

        if (user.Role != UserRole.Manager)
        {
            throw new BusinessValidationException("Only managers can perform this action.");
        }
    }

    private static void ValidateAllocationRequest(CreateAllocationRequest request)
    {
        if (request.EmployeeId <= 0 || request.ProjectId <= 0)
        {
            throw new BusinessValidationException("Employee ID and Project ID are required.");
        }

        if (request.UtilisationPercent <= 0 || request.UtilisationPercent > 100)
        {
            throw new BusinessValidationException("Utilisation must be between 1 and 100.");
        }

        if (request.FromDate.Date >= request.ToDate.Date)
        {
            throw new BusinessValidationException("From date must be before to date.");
        }

        var today = DateTime.UtcNow.Date;

        if (request.ToDate.Date <= today)
        {
            throw new BusinessValidationException("To date must be after today.");
        }
    }

    private async Task<Employee> GetTeamEmployeeOrThrowAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);

        var employee = await _employeeRepository.GetByIdWithDetailsAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new BusinessValidationException("Employee not found or inactive.");
        }

        if (employee.User.Role != UserRole.Employee)
        {
            throw new BusinessValidationException("Only employees can be allocated to projects.");
        }

        if (employee.ManagerId != managerUserId)
        {
            throw new BusinessValidationException("Employee is not assigned to your team.");
        }

        return employee;
    }

    private async Task ValidateProjectForAllocationAsync(
        CreateAllocationRequest request,
        int managerUserId,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdForManagerAsync(
            request.ProjectId,
            managerUserId,
            cancellationToken);

        if (project is null)
        {
            throw new BusinessValidationException("Project not found or not assigned to you.");
        }

        if (project.Status is not (ProjectStatus.Active or ProjectStatus.Planned))
        {
            throw new BusinessValidationException("Project must be in ACTIVE or PLANNED status.");
        }

        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;

        if (fromDate < project.StartDate.Date || toDate > project.EndDate.Date)
        {
            throw new BusinessValidationException("Allocation dates must fall within the project timeline.");
        }
    }

    private static string FormatUtilisationNote(int currentUtilisation)
    {
        return currentUtilisation == 0
            ? "fully on bench"
            : $"{100 - currentUtilisation}% available";
    }

    private static string FormatSkills(IEnumerable<EmployeeSkill> skills)
    {
        var skillNames = skills
            .Select(skill => skill.Skill.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        return skillNames.Count == 0 ? "(none)" : string.Join(", ", skillNames);
    }

    private static string FormatAvailability(int usedPercent, int availablePercent)
    {
        return usedPercent >= 100
            ? "FULL"
            : $"{availablePercent}% free";
    }

    private static string FormatCurrentStatus(int usedPercent)
    {
        if (usedPercent == 0)
        {
            return "BENCH (100%)";
        }

        return $"ALLOCATED ({usedPercent}%)";
    }

    private static string GetRecentActivityTags(Employee employee)
    {
        var fourWeeksAgo = DateTime.UtcNow.Date.AddDays(-28);

        var tags = employee.Timesheets
            .Where(timesheet => timesheet.WeekStartDate.Date >= fourWeeksAgo)
            .SelectMany(timesheet => timesheet.Entries)
            .SelectMany(entry => entry.ActivityTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tags.Count == 0 ? "(none)" : string.Join(", ", tags);
    }

    private static string FormatProjectStatus(ProjectStatus status)
    {
        return status == ProjectStatus.OnHold
            ? "ON_HOLD"
            : status.ToString().ToUpperInvariant();
    }

    private static string FormatHealthStatus(ProjectHealthStatus status)
    {
        return status switch
        {
            ProjectHealthStatus.AtRisk => "AT RISK",
            ProjectHealthStatus.Attention => "ATTENTION",
            _ => "ON TRACK"
        };
    }

    private static IReadOnlyList<Allocation> GetScheduledAllocations(Project project, DateTime today)
    {
        return project.Allocations
            .Where(allocation =>
                allocation.ToDate.Date > today &&
                allocation.Employee.IsActive &&
                allocation.Employee.User.Role == UserRole.Employee)
            .OrderBy(allocation => allocation.Employee.FullName)
            .ToList();
    }

    private static ManagerProjectMilestoneDto MapMilestone(Milestone milestone, int rowNumber, DateTime today)
    {
        var isOverdue = milestone.DueDate.Date < today && milestone.Status != MilestoneStatus.Done;
        var status = FormatMilestoneStatus(milestone.Status);

        if (isOverdue)
        {
            status += " OVERDUE ⚠";
        }

        return new ManagerProjectMilestoneDto
        {
            RowNumber = rowNumber,
            Title = milestone.Title,
            DueDate = milestone.DueDate.ToString("dd-MMM-yy"),
            Status = status
        };
    }

    private static string FormatMilestoneStatus(MilestoneStatus status)
    {
        return status switch
        {
            MilestoneStatus.NotStarted => "NOT_STARTED",
            MilestoneStatus.InProgress => "IN_PROGRESS",
            MilestoneStatus.Done => "DONE",
            _ => status.ToString().ToUpperInvariant()
        };
    }

    private static List<ManagerProjectRiskFlagDto> BuildProjectRiskFlags(
        Project project,
        IReadOnlyList<Allocation> allocations,
        DateTime today)
    {
        var flags = new List<ManagerProjectRiskFlagDto>();
        var lastWeekStart = today.AddDays(-7);

        foreach (var milestone in project.Milestones.Where(m => m.DueDate.Date < today && m.Status != MilestoneStatus.Done))
        {
            var daysOverdue = (today - milestone.DueDate.Date).Days;
            flags.Add(new ManagerProjectRiskFlagDto
            {
                IsPositive = false,
                Message = $"{milestone.Title} milestone is {daysOverdue} days overdue"
            });
        }

        foreach (var allocation in allocations.Where(a =>
                     AllocationDateRules.IsCurrentlyActive(a.FromDate, a.ToDate, today)))
        {
            var expectedHours = allocation.UtilisationPercent * SystemDefaults.MaxWeeklyHours / 100;
            var loggedHours = project.TimesheetEntries
                .Where(entry =>
                    entry.Timesheet.EmployeeId == allocation.EmployeeId &&
                    entry.Timesheet.WeekStartDate.Date >= lastWeekStart.AddDays(-6))
                .Sum(entry => entry.Hours);

            if (loggedHours < expectedHours)
            {
                flags.Add(new ManagerProjectRiskFlagDto
                {
                    IsPositive = false,
                    Message =
                        $"{allocation.Employee.FullName} logged only {loggedHours:0} hrs last week " +
                        $"(expected {expectedHours:0} hrs)"
                });
            }
        }

        if (allocations.Count > 0)
        {
            flags.Add(new ManagerProjectRiskFlagDto
            {
                IsPositive = true,
                Message = "Resources are correctly allocated"
            });
        }

        return flags;
    }

    private static string FormatTimesheetStatus(Timesheet? timesheet)
    {
        if (timesheet is null || timesheet.Status == TimesheetStatus.Missed)
        {
            return "MISSED";
        }

        return "SUBMITTED";
    }
}
