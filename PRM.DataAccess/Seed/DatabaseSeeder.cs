using Microsoft.EntityFrameworkCore;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;
using PRM.DataAccess.Context;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.DataAccess.Seed;

public class DatabaseSeeder : IDataSeeder
{
    private readonly PrmDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(PrmDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedSystemConfigAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roleDefinitions = new[]
        {
            new { Name = RoleNames.Admin, Description = "System administrator" },
            new { Name = RoleNames.Manager, Description = "Project manager" },
            new { Name = RoleNames.Employee, Description = "Individual contributor" }
        };
        foreach (var roleDefinition in roleDefinitions)
        {
            var exists = await _context.Roles
                .AnyAsync(role => role.RoleName == roleDefinition.Name, cancellationToken);
            if (!exists)
            {
                _context.Roles.Add(new Role
                {
                    RoleName = roleDefinition.Name,
                    Description = roleDefinition.Description
                });
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSystemConfigAsync(CancellationToken cancellationToken)
    {
        var configExists = await _context.SystemConfigs
            .AnyAsync(cancellationToken);
        if (configExists)
        {
            return;
        }
        _context.SystemConfigs.Add(new SystemConfig
        {
            LlmProvider = LlmProvider.Gemini,
            LlmApiKey = string.Empty,
            SchedulerIntervalHours = SystemDefaults.SchedulerIntervalHours,
            MaxWeeklyHours = SystemDefaults.MaxWeeklyHours
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var adminRole = await _context.Roles
            .FirstAsync(role => role.RoleName == RoleNames.Admin, cancellationToken);
        var adminExists = await _context.UserRoles
            .AnyAsync(userRole => userRole.RoleId == adminRole.Id, cancellationToken);
        if (adminExists)
        {
            return;
        }
        var adminUser = new User
        {
            FullName = SeedConstants.AdminFullName,
            Email = SeedConstants.AdminEmail,
            Username = SeedConstants.AdminUsername,
            PasswordHash = _passwordHasher.Hash(SeedConstants.AdminDefaultPassword),
            IsActive = true,
            ForcePasswordChange = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken);
        _context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
