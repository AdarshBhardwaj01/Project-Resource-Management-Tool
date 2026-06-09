using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.DataAccess.Context;
using PRM.Models.Entities;
using PRM.Models.Enums;

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
            user => user.Role == UserRole.Admin,
            cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await DbSet.FirstOrDefaultAsync(
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
            .OrderBy(user => user.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> FindByUsernameOrIdAsync(string usernameOrId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usernameOrId);

        if (int.TryParse(usernameOrId, out var userId))
        {
            return await DbSet.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        }

        return await GetByUsernameAsync(usernameOrId, cancellationToken);
    }
}
