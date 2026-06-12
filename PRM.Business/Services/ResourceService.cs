using AutoMapper;
using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Resources;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IPrmSchedulerService _prmSchedulerService;
    private readonly IMapper _mapper;

    public ResourceService(
        IResourceRepository resourceRepository,
        IUserRepository userRepository,
        ISkillRepository skillRepository,
        IPrmSchedulerService prmSchedulerService,
        IMapper mapper)
    {
        _resourceRepository = resourceRepository;
        _userRepository = userRepository;
        _skillRepository = skillRepository;
        _prmSchedulerService = prmSchedulerService;
        _mapper = mapper;
    }

    public async Task<string> CreateResourceAsync(CreateResourceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            throw new BusinessValidationException("A valid User ID is required.");
        }
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("User ID does not exist or is inactive.");
        }
        if (!UserRoleHelper.HasRole(user, ApplicationRole.Manager) &&
            !UserRoleHelper.HasRole(user, ApplicationRole.Employee))
        {
            throw new BusinessValidationException("User must have role EMPLOYEE or MANAGER.");
        }
        if (await _resourceRepository.ExistsActiveByUserIdAsync(request.UserId, cancellationToken))
        {
            throw new BusinessValidationException("This user already has a resource profile.");
        }
        if (await _resourceRepository.ExistsByUserIdAsync(request.UserId, cancellationToken))
        {
            var restored = await _resourceRepository.RestoreInactiveByUserIdAsync(request.UserId, cancellationToken);
            if (!restored)
            {
                throw new BusinessValidationException("Unable to restore resource profile.");
            }
            return "Resource profile reactivated successfully.";
        }
        var resource = _mapper.Map<Resource>(request);
        await _resourceRepository.AddAsync(resource, cancellationToken);
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return "Resource created successfully.";
    }

    public async Task<ResourceListResponse> GetAllResourcesAsync(
        string? status,
        string? department,
        CancellationToken cancellationToken = default)
    {
        ResourceStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ResourceStatus>(status, true, out var parsedStatus))
            {
                throw new BusinessValidationException("Invalid status filter.");
            }
            statusFilter = parsedStatus;
        }
        await _prmSchedulerService.RecomputeAllResourcesAsync(cancellationToken);
        var resources = await _resourceRepository.GetAllAsync(statusFilter, department, cancellationToken);
        var resourceDtos = _mapper.Map<List<ResourceListItemDto>>(resources);
        return new ResourceListResponse
        {
            Resources = resourceDtos,
            Total = resourceDtos.Count,
            AllocatedCount = resourceDtos.Count(resource => resource.Status == "ALLOCATED"),
            BenchCount = resourceDtos.Count(resource => resource.Status == "BENCH")
        };
    }

    public async Task<ResourceDetailDto> GetResourceAsync(int userId, CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var activeAllocations = await _resourceRepository.GetActiveAllocationsAsync(userId, cancellationToken);
        return new ResourceDetailDto
        {
            UserId = resource.UserId,
            FullName = resource.User.FullName,
            Email = resource.User.Email,
            Department = resource.User.Department,
            Designation = resource.User.Designation,
            Status = resource.Status.ToString().ToUpperInvariant(),
            UtilisationPercent = resource.UtilisationPercent,
            ActiveAllocations = activeAllocations.Select(allocation => new ResourceAllocationSummaryDto
            {
                ProjectName = allocation.Project.Name,
                UtilisationPercent = allocation.UtilisationPercent,
                ToDate = allocation.ToDate.ToString("dd-MMM-yy")
            }).ToList()
        };
    }

    public async Task<string> UpdateResourceAsync(int userId, UpdateResourceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateResourceRequest(request);
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        resource.User.Department = request.Department.Trim();
        resource.User.Designation = request.Designation.Trim();
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return "Resource updated successfully.";
    }

    public async Task<string> DeactivateResourceAsync(int userId, CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByUserIdWithDetailsAsync(userId, cancellationToken);
        if (resource is null || !resource.User.IsActive)
        {
            throw new BusinessValidationException("Resource not found or already deactivated.");
        }
        var today = DateTime.UtcNow.Date;
        var activeAllocations = resource.Allocations
            .Where(allocation => AllocationDateRules.IsCurrentlyActive(
                allocation.FromDate,
                allocation.ToDate,
                today))
            .ToList();
        foreach (var allocation in activeAllocations)
        {
            allocation.ToDate = today;
        }
        resource.Status = ResourceStatus.Bench;
        var user = await _userRepository.GetByIdAsync(resource.UserId, cancellationToken);
        if (user is null)
        {
            throw new BusinessValidationException("Linked user account not found.");
        }
        user.IsActive = false;
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return "Resource deactivated.";
    }

    public async Task<IReadOnlyList<ResourceSkillDto>> GetResourceSkillsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByUserIdWithDetailsAsync(userId, cancellationToken);
        if (resource is null || !resource.User.IsActive)
        {
            throw new BusinessValidationException("Resource not found.");
        }
        return resource.Skills
            .OrderBy(skill => skill.Skill.Name)
            .Select(skill => _mapper.Map<ResourceSkillDto>(skill))
            .ToList();
    }

    public async Task<string> AddResourceSkillAsync(
        int userId,
        AddResourceSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAddSkillRequest(request);
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var skill = await GetOrCreateSkillAsync(request.SkillName.Trim(), cancellationToken);
        var existingSkill = resource.Skills.FirstOrDefault(item => item.SkillId == skill.Id);
        if (existingSkill is not null)
        {
            throw new BusinessValidationException("Skill already assigned to this resource.");
        }
        resource.Skills.Add(new ResourceSkill
        {
            UserId = resource.UserId,
            SkillId = skill.Id,
            Category = (SkillCategory)request.Category,
            ProficiencyLevel = (ProficiencyLevel)request.ProficiencyLevel
        });
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return "Skill added.";
    }

    public async Task<string> UpdateResourceSkillAsync(
        int userId,
        int skillId,
        UpdateResourceSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(ProficiencyLevel), request.ProficiencyLevel))
        {
            throw new BusinessValidationException("Invalid proficiency level.");
        }
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var resourceSkill = resource.Skills.FirstOrDefault(item => item.SkillId == skillId);
        if (resourceSkill is null)
        {
            throw new BusinessValidationException("Skill not found for this resource.");
        }
        resourceSkill.ProficiencyLevel = (ProficiencyLevel)request.ProficiencyLevel;
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return "Proficiency level updated.";
    }

    public async Task<string> RemoveResourceSkillAsync(int userId, int skillId, CancellationToken cancellationToken = default)
    {
        var resource = await GetActiveResourceOrThrowAsync(userId, cancellationToken);
        var resourceSkill = resource.Skills.FirstOrDefault(item => item.SkillId == skillId);
        if (resourceSkill is null)
        {
            throw new BusinessValidationException("Skill not found for this resource.");
        }
        resource.Skills.Remove(resourceSkill);
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return "Skill removed.";
    }

    public async Task<string> AssignManagerAsync(
        AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ResourceUserId <= 0 || request.ManagerUserId <= 0)
        {
            throw new BusinessValidationException("Resource User ID and Manager User ID are required.");
        }
        var resource = await _resourceRepository.GetActiveResourceByUserIdAsync(
            request.ResourceUserId,
            cancellationToken);
        if (resource is null)
        {
            throw new BusinessValidationException(
                "Resource profile not found for the given User ID, or the user is inactive.");
        }
        if (!UserRoleHelper.HasRole(resource.User, ApplicationRole.Employee))
        {
            throw new BusinessValidationException("The user must have role EMPLOYEE.");
        }
        var manager = await _userRepository.GetByIdWithRolesAsync(request.ManagerUserId, cancellationToken);
        if (manager is null || !manager.IsActive)
        {
            throw new BusinessValidationException("Manager User ID does not exist or is inactive.");
        }
        if (!UserRoleHelper.HasRole(manager, ApplicationRole.Manager))
        {
            throw new BusinessValidationException("The manager user must have role MANAGER.");
        }
        resource.ManagerUserId = request.ManagerUserId;
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return $"Manager assigned. {resource.User.FullName} is now on {manager.FullName}'s team.";
    }

    private async Task<Resource> GetActiveResourceOrThrowAsync(int userId, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByUserIdWithDetailsAsync(userId, cancellationToken);
        if (resource is null || !resource.User.IsActive)
        {
            throw new BusinessValidationException("Resource not found.");
        }
        return resource;
    }

    private async Task<Skill> GetOrCreateSkillAsync(string skillName, CancellationToken cancellationToken)
    {
        var existingSkill = await _skillRepository.GetByNameAsync(skillName, cancellationToken);
        if (existingSkill is not null)
        {
            return existingSkill;
        }
        var skill = new Skill { Name = skillName };
        await _skillRepository.AddAsync(skill, cancellationToken);
        await _skillRepository.SaveChangesAsync(cancellationToken);
        return skill;
    }

    private static void ValidateUpdateResourceRequest(UpdateResourceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Department)
            || string.IsNullOrWhiteSpace(request.Designation))
        {
            throw new BusinessValidationException("All fields are mandatory.");
        }
    }

    private static void ValidateAddSkillRequest(AddResourceSkillRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.SkillName))
        {
            throw new BusinessValidationException("Skill name is required.");
        }
        if (!Enum.IsDefined(typeof(SkillCategory), request.Category))
        {
            throw new BusinessValidationException("Invalid skill category.");
        }
        if (!Enum.IsDefined(typeof(ProficiencyLevel), request.ProficiencyLevel))
        {
            throw new BusinessValidationException("Invalid proficiency level.");
        }
    }
}
