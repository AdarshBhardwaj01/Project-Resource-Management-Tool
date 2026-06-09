using PRM.Models.Entities;

using PRM.Models.Enums;



namespace PRM.Business.Interfaces.Repositories;



public interface IEmployeeRepository : IRepository<Employee>

{

    Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default);



    Task<bool> ExistsActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);



    Task<bool> RestoreInactiveByUserIdAsync(

        int userId,

        string fullName,

        string email,

        string department,

        string designation,

        CancellationToken cancellationToken = default);



    Task<bool> ReactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default);



    Task<bool> DeactivateByUserIdAsync(int userId, CancellationToken cancellationToken = default);



    Task<Employee?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);



    Task<Employee?> GetByIdForSchedulerUpdateAsync(int id, CancellationToken cancellationToken = default);



    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);



    Task<Employee?> GetActiveEmployeeByUserIdAsync(int userId, CancellationToken cancellationToken = default);



    Task<IReadOnlyList<Employee>> GetAllAsync(

        EmployeeStatus? status,

        string? department,

        CancellationToken cancellationToken = default);



    Task<IReadOnlyList<Allocation>> GetActiveAllocationsAsync(int employeeId, CancellationToken cancellationToken = default);



    Task<IReadOnlyList<Employee>> GetEmployeesWithSkillsForDashboardAsync(

        int managerUserId,

        CancellationToken cancellationToken = default);



    Task<IReadOnlyList<Employee>> GetTeamEmployeesWithAllocationsAsync(

        int managerUserId,

        DateTime weekStart,

        DateTime weekEnd,

        CancellationToken cancellationToken = default);



    Task<Employee?> GetEmployeeForDrillDownAsync(

        int employeeId,

        int managerUserId,

        CancellationToken cancellationToken = default);



    Task<IReadOnlyList<Employee>> GetAllActiveWithAllocationsAsync(CancellationToken cancellationToken = default);



    Task<IReadOnlyList<int>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default);



    Task<bool> IsAssignedToManagerAsync(

        int employeeId,

        int managerUserId,

        CancellationToken cancellationToken = default);

}


