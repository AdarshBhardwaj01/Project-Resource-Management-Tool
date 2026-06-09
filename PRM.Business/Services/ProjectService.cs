using AutoMapper;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Projects;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMilestoneRepository _milestoneRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IEmployeeStatusSchedulerService _employeeStatusSchedulerService;

    public ProjectService(
        IProjectRepository projectRepository,
        IMilestoneRepository milestoneRepository,
        IUserRepository userRepository,
        IMapper mapper,
        IEmployeeStatusSchedulerService employeeStatusSchedulerService)
    {
        _projectRepository = projectRepository;
        _milestoneRepository = milestoneRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _employeeStatusSchedulerService = employeeStatusSchedulerService;
    }

    public async Task<string> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateProjectRequest(request);
        await ValidateManagerAsync(request.ManagerId, cancellationToken);

        if (await _projectRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            throw new BusinessValidationException("Project name already exists.");
        }

        var project = _mapper.Map<Project>(request);
        project.ManagerId = request.ManagerId;

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        await _employeeStatusSchedulerService.RecomputeEmployeeStatusByUserIdAsync(
            request.ManagerId,
            cancellationToken);

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return "Project created successfully.";
    }

    public async Task<ProjectListResponse> GetAllProjectsAsync(string? status, CancellationToken cancellationToken = default)
    {
        ProjectStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProjectStatus>(status, true, out var parsedStatus))
            {
                throw new BusinessValidationException("Invalid status filter.");
            }

            statusFilter = parsedStatus;
        }

        var projects = await _projectRepository.GetAllAsync(statusFilter, cancellationToken);
        var projectDtos = _mapper.Map<List<ProjectListItemDto>>(projects);

        return new ProjectListResponse
        {
            Projects = projectDtos,
            Total = projectDtos.Count,
            PlannedCount = projectDtos.Count(project => project.Status == "PLANNED"),
            ActiveCount = projectDtos.Count(project => project.Status == "ACTIVE"),
            OnHoldCount = projectDtos.Count(project => project.Status == "ON_HOLD")
        };
    }

    public async Task<ProjectDetailDto> GetProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(id, cancellationToken);

        return new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate.ToString("dd-MMM-yy"),
            EndDate = project.EndDate.ToString("dd-MMM-yy"),
            Status = FormatProjectStatus(project.Status),
            HealthStatus = project.HealthStatus.ToString().ToUpperInvariant(),
            ManagerId = project.ManagerId,
            ManagerName = project.Manager.FullName,
            Milestones = project.Milestones
                .OrderBy(milestone => milestone.SortOrder)
                .Select(milestone => _mapper.Map<MilestoneItemDto>(milestone))
                .ToList()
        };
    }

    public async Task<string> UpdateProjectAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateProjectRequest(request);

        var project = await GetProjectOrThrowAsync(id, cancellationToken);

        if (!string.Equals(project.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            && await _projectRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            throw new BusinessValidationException("Project name already exists.");
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description.Trim();
        project.StartDate = request.StartDate.Date;
        project.EndDate = request.EndDate.Date;
        project.Status = (ProjectStatus)request.Status;

        ValidateProjectDates(project.StartDate, project.EndDate);
        ValidateMilestonesWithinProjectDates(project);

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return "Project updated successfully.";
    }

    public async Task<string> AddMilestoneAsync(
        int projectId,
        CreateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateMilestoneTitle(request.Title);

        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);
        var dueDate = request.DueDate.Date;

        if (dueDate < project.StartDate || dueDate > project.EndDate)
        {
            throw new BusinessValidationException("Milestone due date must fall within the project timeline.");
        }

        var sortOrder = request.SortOrder > 0
            ? request.SortOrder
            : await _milestoneRepository.GetMaxSortOrderAsync(projectId, cancellationToken) + 1;

        project.Milestones.Add(new Milestone
        {
            ProjectId = project.Id,
            Title = request.Title.Trim(),
            DueDate = dueDate,
            Status = MilestoneStatus.NotStarted,
            SortOrder = sortOrder
        });

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return "Milestone added.";
    }

    public async Task<string> UpdateMilestoneAsync(
        int projectId,
        int milestoneId,
        UpdateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateMilestoneTitle(request.Title);

        if (!Enum.IsDefined(typeof(MilestoneStatus), request.Status))
        {
            throw new BusinessValidationException("Invalid milestone status.");
        }

        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);
        var milestone = project.Milestones.FirstOrDefault(item => item.Id == milestoneId);

        if (milestone is null)
        {
            throw new BusinessValidationException("Milestone not found for this project.");
        }

        var dueDate = request.DueDate.Date;

        if (dueDate < project.StartDate || dueDate > project.EndDate)
        {
            throw new BusinessValidationException("Milestone due date must fall within the project timeline.");
        }

        milestone.Title = request.Title.Trim();
        milestone.DueDate = dueDate;
        milestone.Status = (MilestoneStatus)request.Status;
        milestone.SortOrder = request.SortOrder > 0 ? request.SortOrder : milestone.SortOrder;

        await _projectRepository.SaveChangesAsync(cancellationToken);

        return "Milestone updated.";
    }

    private async Task<Project> GetProjectOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (project is null)
        {
            throw new BusinessValidationException("Project not found.");
        }

        return project;
    }

    private async Task ValidateManagerAsync(int managerId, CancellationToken cancellationToken)
    {
        var manager = await _userRepository.GetByIdAsync(managerId, cancellationToken);

        if (manager is null || !manager.IsActive)
        {
            throw new BusinessValidationException("Manager ID does not exist or is inactive.");
        }

        if (manager.Role != UserRole.Manager)
        {
            throw new BusinessValidationException("Selected user must have the MANAGER role.");
        }
    }

    private static void ValidateCreateProjectRequest(CreateProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Description)
            || request.ManagerId <= 0)
        {
            throw new BusinessValidationException("All fields are mandatory.");
        }

        if (!Enum.IsDefined(typeof(ProjectStatus), request.Status))
        {
            throw new BusinessValidationException("Invalid project status.");
        }

        ValidateProjectDates(request.StartDate.Date, request.EndDate.Date);
    }

    private static void ValidateUpdateProjectRequest(UpdateProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            throw new BusinessValidationException("All fields are mandatory.");
        }

        if (!Enum.IsDefined(typeof(ProjectStatus), request.Status))
        {
            throw new BusinessValidationException("Invalid project status.");
        }

        ValidateProjectDates(request.StartDate.Date, request.EndDate.Date);
    }

    private static void ValidateProjectDates(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new BusinessValidationException("Start date cannot be after end date.");
        }
    }

    private static void ValidateMilestoneTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BusinessValidationException("Milestone title is required.");
        }
    }

    private static void ValidateMilestonesWithinProjectDates(Project project)
    {
        var invalidMilestone = project.Milestones.FirstOrDefault(
            milestone => milestone.DueDate.Date < project.StartDate || milestone.DueDate.Date > project.EndDate);

        if (invalidMilestone is not null)
        {
            throw new BusinessValidationException(
                $"Milestone '{invalidMilestone.Title}' falls outside the updated project timeline.");
        }
    }

    private static string FormatProjectStatus(ProjectStatus status)
    {
        return status == ProjectStatus.OnHold
            ? "ON_HOLD"
            : status.ToString().ToUpperInvariant();
    }
}
