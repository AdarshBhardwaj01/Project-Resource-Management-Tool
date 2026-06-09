using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<bool> AnyAdminExistsAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> FindByUsernameOrIdAsync(string usernameOrId, CancellationToken cancellationToken = default);
}
