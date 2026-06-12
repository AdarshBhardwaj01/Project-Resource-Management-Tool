using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;

namespace PRM.DataAccess.Repositories;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        return await DbSet.FirstOrDefaultAsync(
            role => role.RoleName == roleName,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderBy(role => role.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
