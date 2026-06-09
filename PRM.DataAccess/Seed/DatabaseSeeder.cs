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
        await EnsureEmployeeUtilisationSchemaAsync(cancellationToken);
        await EnsureEmployeeManagerIdSchemaAsync(cancellationToken);

        await SeedSystemConfigAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task EnsureEmployeeUtilisationSchemaAsync(CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[Employees]')
                  AND name = 'UtilisationPercent')
            BEGIN
                ALTER TABLE [Employees]
                ADD [UtilisationPercent] int NOT NULL CONSTRAINT [DF_Employees_UtilisationPercent] DEFAULT 0;
            END
            """,
            cancellationToken);
    }

    private async Task EnsureEmployeeManagerIdSchemaAsync(CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[Employees]')
                  AND name = 'ManagerId')
            BEGIN
                ALTER TABLE [Employees] ADD [ManagerId] int NULL;
                CREATE INDEX [IX_Employees_ManagerId] ON [Employees] ([ManagerId]);
                ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Users_ManagerId]
                    FOREIGN KEY ([ManagerId]) REFERENCES [Users] ([Id]);
            END
            """,
            cancellationToken);
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
        var adminExists = await _context.Users
            .AnyAsync(user => user.Role == UserRole.Admin, cancellationToken);

        if (adminExists)
        {
            return;
        }

        _context.Users.Add(new User
        {
            FullName = SeedConstants.AdminFullName,
            Email = SeedConstants.AdminEmail,
            Username = SeedConstants.AdminUsername,
            PasswordHash = _passwordHasher.Hash(SeedConstants.AdminDefaultPassword),
            Role = UserRole.Admin,
            IsActive = true,
            ForcePasswordChange = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
