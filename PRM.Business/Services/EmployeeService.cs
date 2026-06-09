using AutoMapper;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Employees;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IPrmSchedulerService _prmSchedulerService;
    private readonly IMapper _mapper;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        ISkillRepository skillRepository,
        IPrmSchedulerService prmSchedulerService,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _skillRepository = skillRepository;
        _prmSchedulerService = prmSchedulerService;
        _mapper = mapper;
    }

    public async Task<string> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateEmployeeRequest(request);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new BusinessValidationException("User ID does not exist or is inactive.");
        }

        if (user.Role is not (UserRole.Manager or UserRole.Employee))
        {
            throw new BusinessValidationException("User must have role EMPLOYEE or MANAGER.");
        }

        if (await _employeeRepository.ExistsActiveByUserIdAsync(request.UserId, cancellationToken))
        {
            throw new BusinessValidationException("This user already has an employee profile.");
        }

        if (await _employeeRepository.ExistsByUserIdAsync(request.UserId, cancellationToken))
        {
            var restored = await _employeeRepository.RestoreInactiveByUserIdAsync(
                request.UserId,
                request.FullName,
                request.Email,
                request.Department,
                request.Designation,
                cancellationToken);

            if (!restored)
            {
                throw new BusinessValidationException("Unable to restore employee profile.");
            }

            return "Employee profile reactivated successfully.";
        }

        var employee = _mapper.Map<Employee>(request);
        employee.UserId = request.UserId;

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return "Employee created successfully.";
    }

    public async Task<EmployeeListResponse> GetAllEmployeesAsync(
        string? status,
        string? department,
        CancellationToken cancellationToken = default)
    {
        EmployeeStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EmployeeStatus>(status, true, out var parsedStatus))
            {
                throw new BusinessValidationException("Invalid status filter.");
            }

            statusFilter = parsedStatus;
        }

        await _prmSchedulerService.RecomputeAllEmployeesAsync(cancellationToken);

        var employees = await _employeeRepository.GetAllAsync(statusFilter, department, cancellationToken);
        var employeeDtos = _mapper.Map<List<EmployeeListItemDto>>(employees);

        return new EmployeeListResponse
        {
            Employees = employeeDtos,
            Total = employeeDtos.Count,
            AllocatedCount = employeeDtos.Count(e => e.Status == "ALLOCATED"),
            BenchCount = employeeDtos.Count(e => e.Status == "BENCH")
        };
    }

    public async Task<EmployeeDetailDto> GetEmployeeAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await GetActiveEmployeeOrThrowAsync(id, cancellationToken);
        var activeAllocations = await _employeeRepository.GetActiveAllocationsAsync(id, cancellationToken);

        return new EmployeeDetailDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            FullName = employee.FullName,
            Email = employee.Email,
            Department = employee.Department,
            Designation = employee.Designation,
            Status = employee.Status.ToString().ToUpperInvariant(),
            UtilisationPercent = employee.UtilisationPercent,
            ActiveAllocations = activeAllocations.Select(allocation => new EmployeeAllocationSummaryDto
            {
                ProjectName = allocation.Project.Name,
                UtilisationPercent = allocation.UtilisationPercent,
                ToDate = allocation.ToDate.ToString("dd-MMM-yy")
            }).ToList()
        };
    }

    public async Task<string> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateEmployeeRequest(request);

        var employee = await GetActiveEmployeeOrThrowAsync(id, cancellationToken);

        employee.FullName = request.FullName.Trim();
        employee.Department = request.Department.Trim();
        employee.Designation = request.Designation.Trim();

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return "Employee updated successfully.";
    }

    public async Task<string> DeactivateEmployeeAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new BusinessValidationException("Employee not found or already deactivated.");
        }

        var today = DateTime.UtcNow.Date;
        var activeAllocations = employee.Allocations
            .Where(allocation => AllocationDateRules.IsCurrentlyActive(
                allocation.FromDate,
                allocation.ToDate,
                today))
            .ToList();

        foreach (var allocation in activeAllocations)
        {
            allocation.ToDate = today;
        }

        employee.IsActive = false;
        employee.Status = EmployeeStatus.Bench;

        var user = await _userRepository.GetByIdAsync(employee.UserId, cancellationToken);

        if (user is null)
        {
            throw new BusinessValidationException("Linked user account not found.");
        }

        user.IsActive = false;

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return "Employee deactivated.";
    }

    public async Task<IReadOnlyList<EmployeeSkillDto>> GetEmployeeSkillsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdWithDetailsAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new BusinessValidationException("Employee not found.");
        }

        return employee.Skills
            .OrderBy(skill => skill.Skill.Name)
            .Select(skill => _mapper.Map<EmployeeSkillDto>(skill))
            .ToList();
    }

    public async Task<string> AddEmployeeSkillAsync(
        int employeeId,
        AddEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAddSkillRequest(request);

        var employee = await GetActiveEmployeeOrThrowAsync(employeeId, cancellationToken);
        var skill = await GetOrCreateSkillAsync(request.SkillName.Trim(), cancellationToken);

        var existingSkill = employee.Skills.FirstOrDefault(item => item.SkillId == skill.Id);

        if (existingSkill is not null)
        {
            throw new BusinessValidationException("Skill already assigned to this employee.");
        }

        employee.Skills.Add(new EmployeeSkill
        {
            EmployeeId = employee.Id,
            SkillId = skill.Id,
            Category = (SkillCategory)request.Category,
            ProficiencyLevel = (ProficiencyLevel)request.ProficiencyLevel
        });

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return "Skill added.";
    }

    public async Task<string> UpdateEmployeeSkillAsync(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(ProficiencyLevel), request.ProficiencyLevel))
        {
            throw new BusinessValidationException("Invalid proficiency level.");
        }

        var employee = await GetActiveEmployeeOrThrowAsync(employeeId, cancellationToken);
        var employeeSkill = employee.Skills.FirstOrDefault(item => item.SkillId == skillId);

        if (employeeSkill is null)
        {
            throw new BusinessValidationException("Skill not found for this employee.");
        }

        employeeSkill.ProficiencyLevel = (ProficiencyLevel)request.ProficiencyLevel;

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return "Proficiency level updated.";
    }

    public async Task<string> RemoveEmployeeSkillAsync(int employeeId, int skillId, CancellationToken cancellationToken = default)
    {
        var employee = await GetActiveEmployeeOrThrowAsync(employeeId, cancellationToken);
        var employeeSkill = employee.Skills.FirstOrDefault(item => item.SkillId == skillId);

        if (employeeSkill is null)
        {
            throw new BusinessValidationException("Skill not found for this employee.");
        }

        employee.Skills.Remove(employeeSkill);

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return "Skill removed.";
    }

    public async Task<string> AssignManagerAsync(
        AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmployeeUserId <= 0 || request.ManagerUserId <= 0)
        {
            throw new BusinessValidationException("Employee User ID and Manager User ID are required.");
        }

        var employee = await _employeeRepository.GetActiveEmployeeByUserIdAsync(
            request.EmployeeUserId,
            cancellationToken);

        if (employee is null)
        {
            throw new BusinessValidationException(
                "Employee profile not found for the given User ID, or the user is inactive.");
        }

        if (employee.User.Role != UserRole.Employee)
        {
            throw new BusinessValidationException("The user must have role EMPLOYEE.");
        }

        var manager = await _userRepository.GetByIdAsync(request.ManagerUserId, cancellationToken);

        if (manager is null || !manager.IsActive)
        {
            throw new BusinessValidationException("Manager User ID does not exist or is inactive.");
        }

        if (manager.Role != UserRole.Manager)
        {
            throw new BusinessValidationException("The manager user must have role MANAGER.");
        }

        employee.ManagerId = request.ManagerUserId;

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return $"Manager assigned. {employee.FullName} is now on {manager.FullName}'s team.";
    }

    private async Task<Employee> GetActiveEmployeeOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new BusinessValidationException("Employee not found.");
        }

        return employee;
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

    private static void ValidateCreateEmployeeRequest(CreateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId <= 0
            || string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Department)
            || string.IsNullOrWhiteSpace(request.Designation))
        {
            throw new BusinessValidationException("All fields are mandatory.");
        }

        EmailValidator.Validate(request.Email);
    }

    private static void ValidateUpdateEmployeeRequest(UpdateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Department)
            || string.IsNullOrWhiteSpace(request.Designation))
        {
            throw new BusinessValidationException("All fields are mandatory.");
        }
    }

    private static void ValidateAddSkillRequest(AddEmployeeSkillRequest request)
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
