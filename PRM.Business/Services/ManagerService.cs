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
    private readonly IResourceRepository _resourceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeStatusSchedulerService _employeeStatusSchedulerService;
    private readonly IPrmSchedulerService _prmSchedulerService;
    private readonly IAiService _aiService;

    public ManagerService(
        IResourceRepository employeeRepository,
        IProjectRepository projectRepository,
        IAllocationRepository allocationRepository,
        ITimesheetRepository timesheetRepository,
        IUserRepository userRepository,
        IEmployeeStatusSchedulerService employeeStatusSchedulerService,
        IPrmSchedulerService prmSchedulerService,
        IAiService aiService)
    {
        _resourceRepository = employeeRepository;
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
        await _prmSchedulerService.RecomputeAllResourcesAsync(cancellationToken);
        var resources = await _resourceRepository.GetResourcesWithSkillsForDashboardAsync(
            managerUserId,
            cancellationToken);
        var managerUserIds = (await _projectRepository.GetManagerUserIdsAsync(cancellationToken)).ToHashSet();
        var today = DateTime.UtcNow.Date;
        var benchEmployees = new List<ResourceDashboardBenchItemDto>();
        var activeEmployees = new List<ResourceDashboardActiveItemDto>();
        var overUtilisedCount = 0;
        var partialCount = 0;
        foreach (var resource in resources)
        {
            var usedPercent = ResourceSchedulerHelper.ComputeUtilisationPercent(resource, today);
            var status = ResourceSchedulerHelper.ComputeStatus(resource, today, managerUserIds);
            var skills = FormatSkills(resource.Skills);
            if (usedPercent > 100)
            {
                overUtilisedCount++;
            }
            else if (usedPercent > 0 && usedPercent < 100)
            {
                partialCount++;
            }
            if (status == ResourceStatus.Bench)
            {
                benchEmployees.Add(new ResourceDashboardBenchItemDto
                {
                    Id = resource.UserId,
                    FullName = resource.User.FullName,
                    Department = resource.User.Department,
                    Skills = skills
                });
                continue;
            }
            var availablePercent = 100 - usedPercent;
            activeEmployees.Add(new ResourceDashboardActiveItemDto
            {
                Id = resource.UserId,
                FullName = resource.User.FullName,
                Department = resource.User.Department,
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
                    EmployeeName = allocation.Resource.User.FullName,
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
        await GetTeamResourceOrThrowAsync(managerUserId, request.EmployeeId, cancellationToken);
        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;
        var allocation = new Allocation
        {
            UserId = request.EmployeeId,
            ProjectId = request.ProjectId,
            UtilisationPercent = request.UtilisationPercent,
            FromDate = fromDate,
            ToDate = toDate
        };
        await _allocationRepository.AddAsync(allocation, cancellationToken);
        await _allocationRepository.SaveChangesAsync(cancellationToken);
        await _employeeStatusSchedulerService.RecomputeResourceStatusAsync(
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
        var resource = await _resourceRepository.GetResourceForDrillDownAsync(
            employeeId,
            managerUserId,
            cancellationToken);
        if (resource is null)
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }
        var today = DateTime.UtcNow.Date;
        var scheduledAllocations = resource.Allocations
            .Where(allocation => allocation.ToDate.Date > today)
            .OrderBy(allocation => allocation.Project.Name)
            .ToList();
        var usedPercent = resource.UtilisationPercent;
        return new EmployeeDrillDownDto
        {
            Id = resource.UserId,
            FullName = resource.User.FullName,
            Department = resource.User.Department,
            CurrentStatus = FormatCurrentStatus(usedPercent),
            ProfileSkills = FormatSkills(resource.Skills),
            ActiveAllocations = scheduledAllocations
                .Select(allocation => new EmployeeAllocationDetailDto
                {
                    ProjectName = allocation.Project.Name,
                    UtilisationPercent = allocation.UtilisationPercent,
                    FromDate = allocation.FromDate.ToString("dd-MMM-yy"),
                    ToDate = allocation.ToDate.ToString("dd-MMM-yy")
                })
                .ToList(),
            RecentActivityTags = GetRecentActivityTags(resource)
        };
    }

    public async Task<EmployeeUtilisationPreviewDto> GetEmployeeUtilisationPreviewAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var resource = await GetTeamResourceOrThrowAsync(managerUserId, employeeId, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var displayUtilisation = ResourceSchedulerHelper.ComputeUtilisationPercent(resource, today);
        return new EmployeeUtilisationPreviewDto
        {
            Id = resource.UserId,
            FullName = resource.User.FullName,
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
        var resource = await GetTeamResourceOrThrowAsync(managerUserId, request.EmployeeId, cancellationToken);
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
            EmployeeName = resource.User.FullName,
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
                EmployeeName = allocation.Resource.User.FullName,
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
        if (!UserRoleHelper.HasRole(allocation.Resource.User, ApplicationRole.Employee))
        {
            throw new BusinessValidationException("Only employee allocations can be ended.");
        }
        var today = DateTime.UtcNow.Date;
        if (!AllocationDateRules.IsScheduled(allocation.ToDate, today))
        {
            throw new BusinessValidationException("Allocation is already ended.");
        }
        allocation.ToDate = today;
        await _employeeStatusSchedulerService.RecomputeResourceStatusAsync(
            allocation.UserId,
            allocation.Id,
            cancellationToken);
        await _allocationRepository.SaveChangesAsync(cancellationToken);
        await _prmSchedulerService.RecomputeProjectHealthAsync(
            allocation.ProjectId,
            cancellationToken);
        return
            $"Allocation ended. {allocation.Resource.User.FullName} freed from {allocation.Project.Name} " +
            $"as of {today:dd-MMM-yyyy}.";
    }

    public async Task<ManagerTeamTimesheetsResponse> GetTeamTimesheetsAsync(
        int managerUserId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);
        var weekStart = WeekHelper.GetWeekStartDate(weekStartDate ?? DateTime.UtcNow.Date);
        var weekEnd = WeekHelper.GetWeekWorkingEndDate(weekStart);
        var today = DateTime.UtcNow.Date;
        var teamResources = await _resourceRepository.GetTeamResourcesWithAllocationsAsync(
            managerUserId,
            weekStart,
            weekEnd,
            cancellationToken);
        var userIds = teamResources.Select(resource => resource.UserId).ToList();
        var timesheets = await _timesheetRepository.GetByUserIdsForWeekAsync(
            userIds,
            weekStart,
            cancellationToken);
        var timesheetByUser = timesheets.ToDictionary(timesheet => timesheet.UserId);
        var rows = new List<ManagerTeamTimesheetRowDto>();
        foreach (var resource in teamResources)
        {
            timesheetByUser.TryGetValue(resource.UserId, out var timesheet);
            var weekAllocations = resource.Allocations
                .Where(allocation =>
                    allocation.FromDate.Date <= weekEnd &&
                    allocation.ToDate.Date >= weekStart)
                .OrderBy(allocation => allocation.Project.Name)
                .ToList();
            if (weekAllocations.Count == 0)
            {
                rows.Add(new ManagerTeamTimesheetRowDto
                {
                    EmployeeId = resource.UserId,
                    EmployeeName = resource.User.FullName,
                    ProjectId = 0,
                    ProjectName = "(no active allocation)",
                    Hours = 0,
                    Status = FormatTimesheetStatus(timesheet, weekStart, today)
                });
                continue;
            }
            foreach (var allocation in weekAllocations)
            {
                var entry = timesheet?.Entries.FirstOrDefault(item => item.ProjectId == allocation.ProjectId);
                rows.Add(new ManagerTeamTimesheetRowDto
                {
                    EmployeeId = resource.UserId,
                    EmployeeName = resource.User.FullName,
                    ProjectId = allocation.ProjectId,
                    ProjectName = allocation.Project.Name,
                    Hours = entry is null ? 0 : (int)entry.Hours,
                    Status = FormatTimesheetStatus(timesheet, weekStart, today)
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
        var weekEnd = WeekHelper.GetWeekWorkingEndDate(weekStart);
        var today = DateTime.UtcNow.Date;
        if (!await _resourceRepository.IsAssignedToManagerAsync(employeeId, managerUserId, cancellationToken))
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }
        var resource = await _resourceRepository.GetByUserIdWithDetailsAsync(employeeId, cancellationToken);
        if (resource is null || !resource.User.IsActive)
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }
        var employeeAllocations = resource.Allocations
            .Where(allocation =>
                allocation.FromDate.Date <= weekEnd &&
                allocation.ToDate.Date >= weekStart)
            .OrderBy(allocation => allocation.Project.Name)
            .ToList();
        if (employeeAllocations.Count == 0)
        {
            throw new BusinessValidationException("Employee has no active allocations for the selected week.");
        }
        var timesheet = await _timesheetRepository.GetByUserIdForWeekAsync(
            employeeId,
            weekStart,
            cancellationToken);
        var employeeName = resource.User.FullName;
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
            Status = FormatTimesheetStatus(timesheet, weekStart, today),
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
        await _prmSchedulerService.RecomputeAllResourcesAsync(cancellationToken);
        var resources = request.SearchEntireOrganization
            ? await _resourceRepository.GetAllActiveResourcesWithSkillsAsync(cancellationToken)
            : await _resourceRepository.GetResourcesWithSkillsForDashboardAsync(
                managerUserId,
                cancellationToken);
        var context = await BuildSkillMatchContext(
            request,
            projectName,
            resources,
            cancellationToken);
        if (context.Candidates.Count == 0)
        {
            var parsed = SkillMatchHelper.ParseRequirement(request.Requirement);
            return new SkillMatchResponse
            {
                NoMatchReason = request.RequireSingleEmployeeMatch
                    ? SkillMatchHelper.BuildSingleEmployeeNoMatchReason(parsed, request.SearchEntireOrganization)
                    : request.SearchEntireOrganization
                        ? "No matching employees with the required skills were found in the organization."
                        : SkillMatchHelper.BuildNoMatchReason(parsed)
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

    public async Task<TeamBuildResponse> BuildTeamAsync(
        int managerUserId,
        TeamBuildRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new BusinessValidationException("Team build prompt is required.");
        }
        var roleSlots = TeamBuildRequirementParser.Parse(request.Prompt);
        if (roleSlots.Count == 0)
        {
            throw new BusinessValidationException(
                "Could not identify any roles from the prompt. " +
                "Please describe roles clearly, e.g. '1 Java developer, 1 QA engineer, 1 DevOps engineer'.");
        }
        await _prmSchedulerService.RecomputeAllResourcesAsync(cancellationToken);
        var allResources = await _resourceRepository.GetAllActiveResourcesWithSkillsAsync(cancellationToken);
        var managerUserIds = (await _projectRepository.GetManagerUserIdsAsync(cancellationToken)).ToHashSet();
        var today = DateTime.UtcNow.Date;
        var benchResources = allResources
            .Where(resource => ResourceSchedulerHelper.ComputeStatus(resource, today, managerUserIds) == ResourceStatus.Bench)
            .ToList();
        var assignedUserIds = new HashSet<int>();
        var results = new List<TeamRoleResultDto>();
        var slotNumber = 0;
        foreach (var slot in roleSlots)
        {
            for (var i = 0; i < slot.Count; i++)
            {
                slotNumber++;
                var best = benchResources
                    .Where(resource => !assignedUserIds.Contains(resource.UserId))
                    .Select(resource =>
                    {
                        var skills = FormatSkills(resource.Skills);
                        var score = SkillMatchHelper.ScoreEmployeeSkills(skills, slot.SkillKeywords);
                        var usedPercent = ResourceSchedulerHelper.ComputeUtilisationPercent(resource, today);
                        return new
                        {
                            Resource = resource,
                            Skills = skills,
                            Score = score,
                            UsedPercent = usedPercent,
                            MatchedSkills = SkillMatchHelper.FormatMatchedSkills(skills, slot.SkillKeywords),
                            Availability = SkillMatchHelper.FormatAvailabilityForDate(usedPercent, true, null)
                        };
                    })
                    .Where(item => item.Score > 0)
                    .OrderByDescending(item => item.Score)
                    .ThenBy(item => item.UsedPercent)
                    .FirstOrDefault();
                if (best is not null)
                {
                    assignedUserIds.Add(best.Resource.UserId);
                    results.Add(new TeamRoleResultDto
                    {
                        SlotNumber = slotNumber,
                        RoleLabel = slot.RoleLabel,
                        Filled = true,
                        EmployeeId = best.Resource.UserId,
                        EmployeeName = best.Resource.User.FullName,
                        Department = best.Resource.User.Department,
                        MatchedSkills = best.MatchedSkills,
                        Availability = best.Availability
                    });
                }
                else
                {
                    results.Add(new TeamRoleResultDto
                    {
                        SlotNumber = slotNumber,
                        RoleLabel = slot.RoleLabel,
                        Filled = false,
                        GapReason = BuildTeamGapReason(slot.SkillKeywords, allResources, assignedUserIds, today)
                    });
                }
            }
        }
        return new TeamBuildResponse
        {
            Roles = results,
            FilledCount = results.Count(r => r.Filled),
            GapCount = results.Count(r => !r.Filled)
        };
    }

    private static string BuildTeamGapReason(
        IReadOnlyList<string> skillKeywords,
        IReadOnlyList<Resource> allResources,
        HashSet<int> assignedUserIds,
        DateTime today)
    {
        var withSkill = allResources
            .Where(resource => !assignedUserIds.Contains(resource.UserId))
            .Where(resource => SkillMatchHelper.ScoreEmployeeSkills(FormatSkills(resource.Skills), skillKeywords) > 0)
            .ToList();
        if (withSkill.Count == 0)
        {
            return "No one in the organization has this skill. Consider hiring or training.";
        }
        var soonestFree = withSkill
            .Select(resource =>
            {
                var freeDate = resource.Allocations
                    .Where(allocation => allocation.ToDate.Date > today)
                    .OrderBy(allocation => allocation.ToDate)
                    .FirstOrDefault()?.ToDate.Date;
                return (Resource: resource, FreeDate: freeDate);
            })
            .OrderBy(item => item.FreeDate ?? DateTime.MaxValue)
            .First();
        return soonestFree.FreeDate is not null
            ? $"{soonestFree.Resource.User.FullName} has the required skill(s) but is allocated until " +
              $"{soonestFree.FreeDate.Value:dd-MMM-yyyy}. Plan around their availability."
            : $"{soonestFree.Resource.User.FullName} has the required skill(s) but is currently not on bench.";
    }

    private async Task<AiSkillMatchContext> BuildSkillMatchContext(
        SkillMatchRequest request,
        string? projectName,
        IReadOnlyList<Resource> resources,
        CancellationToken cancellationToken)
    {
        var parsed = SkillMatchHelper.ParseRequirement(request.Requirement);
        var managerUserIds = (await _projectRepository.GetManagerUserIdsAsync(cancellationToken)).ToHashSet();
        var today = DateTime.UtcNow.Date;
        var evaluationDate = parsed.AvailableFromDate ?? today;
        var candidates = new List<AiSkillMatchCandidateDto>();
        var resourcesToConsider = parsed.SkillKeywords.Count > 0
            ? resources.Where(SkillMatchHelper.HasAssignedSkills)
            : resources;
        foreach (var resource in resourcesToConsider)
        {
            var usedPercent = ResourceSchedulerHelper.ComputeUtilisationPercent(resource, evaluationDate);
            var status = ResourceSchedulerHelper.ComputeStatus(resource, evaluationDate, managerUserIds);
            var skills = FormatSkills(resource.Skills);
            var isOnBench = status == ResourceStatus.Bench;
            var availability = SkillMatchHelper.FormatAvailabilityForDate(
                usedPercent,
                isOnBench,
                parsed.AvailableFromDate);
            var candidate = new AiSkillMatchCandidateDto
            {
                EmployeeId = resource.UserId,
                FullName = resource.User.FullName,
                Department = resource.User.Department,
                Skills = skills,
                IsOnBench = isOnBench,
                Availability = availability,
                UtilisationPercent = usedPercent,
                MatchedSkills = SkillMatchHelper.FormatMatchedSkills(skills, parsed.SkillKeywords),
                RecentActivity = SkillMatchHelper.FormatRecentActivity(resource, parsed.SkillKeywords)
            };
            if (!SkillMatchHelper.IsEligibleCandidate(candidate, parsed))
            {
                continue;
            }
            if (request.RequireSingleEmployeeMatch
                && parsed.SkillKeywords.Count > 1
                && !SkillMatchHelper.MatchesAllSkillRequirements(candidate.Skills, parsed.SkillKeywords))
            {
                continue;
            }
            candidates.Add(candidate);
        }
        return new AiSkillMatchContext
        {
            Requirement = request.Requirement,
            ProjectName = projectName,
            MinAvailablePercent = parsed.MinAvailablePercent,
            AvailableFromDate = parsed.AvailableFromDate,
            RequireFullAvailability = parsed.RequireFullAvailability,
            RequireSingleEmployeeMatch = request.RequireSingleEmployeeMatch,
            MaxSuggestions = Math.Max(1, request.MaxSuggestions),
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
        var user = await _userRepository.FindByUsernameOrIdAsync(managerUserId.ToString(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("Manager account not found or inactive.");
        }
        if (!UserRoleHelper.HasRole(user, ApplicationRole.Manager))
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

    private async Task<Resource> GetTeamResourceOrThrowAsync(
        int managerUserId,
        int userId,
        CancellationToken cancellationToken)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);
        var resource = await _resourceRepository.GetByUserIdWithDetailsAsync(userId, cancellationToken);
        if (resource is null || !resource.User.IsActive)
        {
            throw new BusinessValidationException("Employee not found or inactive.");
        }
        if (!UserRoleHelper.HasRole(resource.User, ApplicationRole.Employee))
        {
            throw new BusinessValidationException("Only employees can be allocated to projects.");
        }
        if (resource.ManagerUserId != managerUserId)
        {
            throw new BusinessValidationException("Employee is not assigned to your team.");
        }
        return resource;
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

    private static string FormatSkills(IEnumerable<ResourceSkill> skills)
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
            ? "0% free"
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

    private static string GetRecentActivityTags(Resource employee)
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
                allocation.Resource.User.IsActive &&
                UserRoleHelper.HasRole(allocation.Resource.User, ApplicationRole.Employee))
            .OrderBy(allocation => allocation.Resource.User.FullName)
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
                    entry.Timesheet.UserId == allocation.UserId &&
                    entry.Timesheet.WeekStartDate.Date >= lastWeekStart.AddDays(-6))
                .Sum(entry => entry.Hours);
            if (loggedHours < expectedHours)
            {
                flags.Add(new ManagerProjectRiskFlagDto
                {
                    IsPositive = false,
                    Message =
                        $"{allocation.Resource.User.FullName} logged only {loggedHours:0} hrs last week " +
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

    private static string FormatTimesheetStatus(
        Timesheet? timesheet,
        DateTime weekStartDate,
        DateTime today)
    {
        return TimesheetWorkflowHelper.GetDisplayStatus(timesheet, weekStartDate, today);
    }

    public async Task<IReadOnlyList<FrozenTimesheetItemDto>> GetFrozenTimesheetsAsync(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);
        var frozenTimesheets = await _timesheetRepository.GetFrozenTimesheetsForManagerAsync(
            managerUserId,
            cancellationToken);
        return frozenTimesheets
            .Select((timesheet, index) => new FrozenTimesheetItemDto
            {
                RowNumber = index + 1,
                EmployeeId = timesheet.UserId,
                EmployeeName = timesheet.Resource.User.FullName,
                WeekStartDate = timesheet.WeekStartDate.ToString("dd-MMM-yyyy"),
                Status = "FROZEN",
                ReminderCount = timesheet.ReminderCount
            })
            .ToList();
    }

    public async Task<string> RestoreFrozenTimesheetAsync(
        int managerUserId,
        RestoreFrozenTimesheetRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerUserAsync(managerUserId, cancellationToken);
        if (request.EmployeeId <= 0 || string.IsNullOrWhiteSpace(request.WeekStartDate))
        {
            throw new BusinessValidationException("Employee ID and week start date are required.");
        }
        if (!await _resourceRepository.IsAssignedToManagerAsync(
                request.EmployeeId,
                managerUserId,
                cancellationToken))
        {
            throw new BusinessValidationException("Employee not found on your team.");
        }
        var weekStart = WeekHelper.GetWeekStartDate(
            DateValidator.ParseRequired(request.WeekStartDate, "Week start date"));
        var timesheet = await _timesheetRepository.GetByUserIdForWeekForUpdateAsync(
            request.EmployeeId,
            weekStart,
            cancellationToken);
        if (timesheet is null || !TimesheetWorkflowHelper.IsFrozen(timesheet))
        {
            throw new BusinessValidationException("No frozen timesheet was found for the selected employee and week.");
        }
        timesheet.IsUnlockedByManager = true;
        timesheet.IsFrozen = false;
        timesheet.Status = TimesheetStatus.Pending;
        await _timesheetRepository.SaveChangesAsync(cancellationToken);
        return
            $"Timesheet access restored for {timesheet.Resource?.User?.FullName ?? "employee"} " +
            $"for week starting {weekStart:dd-MMM-yyyy}.";
    }
}
