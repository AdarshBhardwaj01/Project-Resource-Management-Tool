using PRM.Models.DTOs.Employees;

namespace PRM.Business.Interfaces.Services;

public interface IEmployeeService
{
    Task<string> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeListResponse> GetAllEmployeesAsync(string? status, string? department, CancellationToken cancellationToken = default);

    Task<EmployeeDetailDto> GetEmployeeAsync(int id, CancellationToken cancellationToken = default);

    Task<string> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<string> DeactivateEmployeeAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeSkillDto>> GetEmployeeSkillsAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<string> AddEmployeeSkillAsync(int employeeId, AddEmployeeSkillRequest request, CancellationToken cancellationToken = default);

    Task<string> UpdateEmployeeSkillAsync(int employeeId, int skillId, UpdateEmployeeSkillRequest request, CancellationToken cancellationToken = default);

    Task<string> RemoveEmployeeSkillAsync(int employeeId, int skillId, CancellationToken cancellationToken = default);

    Task<string> AssignManagerAsync(AssignManagerRequest request, CancellationToken cancellationToken = default);
}
