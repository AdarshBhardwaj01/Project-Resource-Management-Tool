using Microsoft.EntityFrameworkCore;
using PRM.DataAccess.Context;

namespace PRM.DataAccess.Context;

public class PrmDbContext : DbContext
{
    public PrmDbContext(DbContextOptions<PrmDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Entities.User> Users => Set<Models.Entities.User>();

    public DbSet<Models.Entities.Employee> Employees => Set<Models.Entities.Employee>();

    public DbSet<Models.Entities.Skill> Skills => Set<Models.Entities.Skill>();

    public DbSet<Models.Entities.EmployeeSkill> EmployeeSkills => Set<Models.Entities.EmployeeSkill>();

    public DbSet<Models.Entities.Project> Projects => Set<Models.Entities.Project>();

    public DbSet<Models.Entities.Milestone> Milestones => Set<Models.Entities.Milestone>();

    public DbSet<Models.Entities.Allocation> Allocations => Set<Models.Entities.Allocation>();

    public DbSet<Models.Entities.Timesheet> Timesheets => Set<Models.Entities.Timesheet>();

    public DbSet<Models.Entities.TimesheetEntry> TimesheetEntries => Set<Models.Entities.TimesheetEntry>();

    public DbSet<Models.Entities.SystemConfig> SystemConfigs => Set<Models.Entities.SystemConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrmDbContext).Assembly);
    }
}
