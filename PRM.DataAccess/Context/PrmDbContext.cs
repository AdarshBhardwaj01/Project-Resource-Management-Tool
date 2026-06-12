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

    public DbSet<Models.Entities.Role> Roles => Set<Models.Entities.Role>();

    public DbSet<Models.Entities.UserRole> UserRoles => Set<Models.Entities.UserRole>();

    public DbSet<Models.Entities.Resource> Resources => Set<Models.Entities.Resource>();

    public DbSet<Models.Entities.Skill> Skills => Set<Models.Entities.Skill>();

    public DbSet<Models.Entities.ResourceSkill> ResourceSkills => Set<Models.Entities.ResourceSkill>();

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
