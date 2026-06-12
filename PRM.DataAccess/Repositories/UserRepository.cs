using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.Common.Constants;
using PRM.DataAccess.Context;
using PRM.Models.Entities;

namespace PRM.DataAccess.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(PrmDbContext context)
        : base(context)
    {
    }

    public async Task<bool> AnyAdminExistsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            user => user.UserRoles.Any(userRole => userRole.Role.RoleName == RoleNames.Admin),
            cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return await DbSet
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(
                user => user.Username == username,
                cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return await DbSet.AnyAsync(
            user => user.Username == username,
            cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return await DbSet.AnyAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .OrderBy(user => user.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<User?> FindByUsernameOrIdAsync(string usernameOrId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usernameOrId);
        if (int.TryParse(usernameOrId, out var userId))
        {
            return await DbSet
                .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        }
        return await GetByUsernameAsync(usernameOrId, cancellationToken);
    }

    public async Task<bool> HasRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            user =>
                user.Id == userId &&
                user.UserRoles.Any(userRole => userRole.Role.RoleName == roleName),
            cancellationToken);
    }

    public async Task AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
    {
        var alreadyAssigned = await Context.Set<UserRole>()
            .AnyAsync(
                userRole => userRole.UserId == userId && userRole.RoleId == roleId,
                cancellationToken);
        if (alreadyAssigned)
        {
            return;
        }
        await Context.Set<UserRole>().AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = roleId
        }, cancellationToken);
    }
}
