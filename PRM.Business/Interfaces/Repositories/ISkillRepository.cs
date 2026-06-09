using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Repositories;

public interface ISkillRepository : IRepository<Skill>
{
    Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
