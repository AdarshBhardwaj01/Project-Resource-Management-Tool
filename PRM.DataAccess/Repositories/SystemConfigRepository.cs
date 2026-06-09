using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.Common.Constants;
using PRM.DataAccess.Context;
using PRM.Models.Entities;

namespace PRM.DataAccess.Repositories;

public class SystemConfigRepository : GenericRepository<SystemConfig>, ISystemConfigRepository
{
    public SystemConfigRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<SystemConfig?> GetSingletonAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(config => config.Id == SystemDefaults.SystemConfigId, cancellationToken)
            ?? await DbSet.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SystemConfig?> GetSingletonForUpdateAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(config => config.Id == SystemDefaults.SystemConfigId, cancellationToken)
            ?? await DbSet.FirstOrDefaultAsync(cancellationToken);
    }
}
