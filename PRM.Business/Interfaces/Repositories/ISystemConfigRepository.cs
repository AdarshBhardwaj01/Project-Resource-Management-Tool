using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface ISystemConfigRepository : IRepository<SystemConfig>
{
    Task<SystemConfig?> GetSingletonAsync(CancellationToken cancellationToken = default);
    Task<SystemConfig?> GetSingletonForUpdateAsync(CancellationToken cancellationToken = default);
}
