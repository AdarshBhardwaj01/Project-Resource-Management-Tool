using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;

namespace PRM.Business.Services;

public class EmployeeStatusSchedulerService : IEmployeeStatusSchedulerService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPrmSchedulerService _prmSchedulerService;

    public EmployeeStatusSchedulerService(
        IEmployeeRepository employeeRepository,
        IPrmSchedulerService prmSchedulerService)
    {
        _employeeRepository = employeeRepository;
        _prmSchedulerService = prmSchedulerService;
    }

    public async Task RecomputeEmployeeStatusByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return;
        }

        await RecomputeEmployeeStatusAsync(employee.Id, cancellationToken: cancellationToken);
    }

    public Task RecomputeEmployeeStatusAsync(
        int employeeId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        return _prmSchedulerService.RecomputeEmployeeAsync(
            employeeId,
            excludeAllocationId,
            cancellationToken);
    }
}
