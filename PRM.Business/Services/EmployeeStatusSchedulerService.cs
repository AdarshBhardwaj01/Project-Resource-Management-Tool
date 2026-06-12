using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;

namespace PRM.Business.Services;

public class EmployeeStatusSchedulerService : IEmployeeStatusSchedulerService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IPrmSchedulerService _prmSchedulerService;

    public EmployeeStatusSchedulerService(
        IResourceRepository resourceRepository,
        IPrmSchedulerService prmSchedulerService)
    {
        _resourceRepository = resourceRepository;
        _prmSchedulerService = prmSchedulerService;
    }

    public async Task RecomputeResourceStatusByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (resource is null)
        {
            return;
        }
        await RecomputeResourceStatusAsync(userId, cancellationToken: cancellationToken);
    }

    public Task RecomputeResourceStatusAsync(
        int userId,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        return _prmSchedulerService.RecomputeResourceAsync(
            userId,
            excludeAllocationId,
            cancellationToken);
    }
}
