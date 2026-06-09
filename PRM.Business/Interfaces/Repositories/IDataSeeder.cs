namespace PRM.Business.Interfaces.Repositories;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
