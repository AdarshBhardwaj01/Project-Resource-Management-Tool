using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PRM.DataAccess.Context;

#nullable disable

namespace PRM.DataAccess.Migrations;

[DbContext(typeof(PrmDbContext))]
[Migration("20260528200000_RefactorEmployeeToResource")]
public partial class RefactorEmployeeToResource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Create Roles table ────────────────────────────────────────────
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[Roles]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Roles] (
                    [Id]          int           NOT NULL IDENTITY,
                    [RoleName]    nvarchar(50)  NOT NULL,
                    [Description] nvarchar(200) NOT NULL,
                    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
            END
            """);

        // ── 2. Seed default roles ────────────────────────────────────────────
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Admin')
                INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Admin', N'System administrator');
            IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Manager')
                INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Manager', N'Project manager');
            IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Employee')
                INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Employee', N'Individual contributor');
            """);

        // ── 3. Create USER_ROLE junction table ───────────────────────────────
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[USER_ROLE]', N'U') IS NULL
            BEGIN
                CREATE TABLE [USER_ROLE] (
                    [Id]     int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [RoleId] int NOT NULL,
                    CONSTRAINT [PK_USER_ROLE] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_USER_ROLE_Users_UserId]  FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_USER_ROLE_Roles_RoleId]  FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_USER_ROLE_UserId_RoleId] ON [USER_ROLE] ([UserId], [RoleId]);
            END
            """);

        // ── 4. Migrate Users.Role → USER_ROLE ───────────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Role')
            BEGIN
                INSERT INTO [USER_ROLE] ([UserId], [RoleId])
                SELECT u.[Id], r.[Id]
                FROM   [Users] u
                INNER JOIN [Roles] r ON r.[RoleName] = u.[Role]
                WHERE  NOT EXISTS (
                    SELECT 1 FROM [USER_ROLE] ur
                    WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]);
            END
            """);

        // ── 5. Create Resources table from Employees ─────────────────────────
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[Resources]', N'U') IS NULL AND OBJECT_ID(N'[Employees]', N'U') IS NOT NULL
            BEGIN
                CREATE TABLE [Resources] (
                    [UserId]            int          NOT NULL,
                    [Department]        nvarchar(50) NOT NULL,
                    [Designation]       nvarchar(50) NOT NULL,
                    [Status]            nvarchar(20) NOT NULL,
                    [ManagerUserId]     int          NULL,
                    [UtilisationPercent] int         NOT NULL CONSTRAINT [DF_Resources_UtilisationPercent] DEFAULT 0,
                    CONSTRAINT [PK_Resources]               PRIMARY KEY ([UserId]),
                    CONSTRAINT [FK_Resources_Users_UserId]  FOREIGN KEY ([UserId])        REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Resources_Users_ManagerUserId] FOREIGN KEY ([ManagerUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Resources_ManagerUserId] ON [Resources] ([ManagerUserId]);

                INSERT INTO [Resources] ([UserId], [Department], [Designation], [Status], [ManagerUserId], [UtilisationPercent])
                SELECT e.[UserId], e.[Department], e.[Designation], e.[Status], e.[ManagerId], e.[UtilisationPercent]
                FROM   [Employees] e;
            END
            """);

        // ── 6a. Allocations – add UserId column ──────────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS     (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Allocations]') AND name = 'EmployeeId')
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Allocations]') AND name = 'UserId')
            BEGIN
                ALTER TABLE [Allocations] ADD [UserId] int NULL;
            END
            """);

        // ── 6b. Allocations – copy data from Employees ───────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Allocations]') AND name = 'UserId')
               AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Allocations]') AND name = 'EmployeeId')
            BEGIN
                UPDATE a
                SET    a.[UserId] = e.[UserId]
                FROM   [Allocations] a
                INNER JOIN [Employees] e ON e.[Id] = a.[EmployeeId];
            END
            """);

        // ── 6c. Allocations – make UserId NOT NULL ───────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns c
                       WHERE c.object_id = OBJECT_ID(N'[Allocations]') AND c.name = 'UserId' AND c.is_nullable = 1)
            BEGIN
                ALTER TABLE [Allocations] ALTER COLUMN [UserId] int NOT NULL;
            END
            """);

        // ── 6d. Allocations – drop old FK, index, and EmployeeId column ──────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Allocations]') AND name = 'EmployeeId')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Allocations_Employees_EmployeeId')
                    ALTER TABLE [Allocations] DROP CONSTRAINT [FK_Allocations_Employees_EmployeeId];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Allocations_EmployeeId' AND object_id = OBJECT_ID(N'[Allocations]'))
                    DROP INDEX [IX_Allocations_EmployeeId] ON [Allocations];
                ALTER TABLE [Allocations] DROP COLUMN [EmployeeId];
            END
            """);

        // ── 6e. Allocations – add FK to Resources and new index ──────────────
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Allocations_Resources_UserId')
               AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Allocations]') AND name = 'UserId')
            BEGIN
                ALTER TABLE [Allocations] ADD CONSTRAINT [FK_Allocations_Resources_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [Resources] ([UserId]) ON DELETE NO ACTION;
                CREATE INDEX [IX_Allocations_UserId] ON [Allocations] ([UserId]);
            END
            """);

        // ── 7a. Timesheets – add UserId column ───────────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS     (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'EmployeeId')
               AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'UserId')
            BEGIN
                ALTER TABLE [Timesheets] ADD [UserId] int NULL;
            END
            """);

        // ── 7b. Timesheets – copy data from Employees ────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'UserId')
               AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'EmployeeId')
            BEGIN
                UPDATE t
                SET    t.[UserId] = e.[UserId]
                FROM   [Timesheets] t
                INNER JOIN [Employees] e ON e.[Id] = t.[EmployeeId];
            END
            """);

        // ── 7c. Timesheets – make UserId NOT NULL ────────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns c
                       WHERE c.object_id = OBJECT_ID(N'[Timesheets]') AND c.name = 'UserId' AND c.is_nullable = 1)
            BEGIN
                ALTER TABLE [Timesheets] ALTER COLUMN [UserId] int NOT NULL;
            END
            """);

        // ── 7d. Timesheets – drop old FK, index, and EmployeeId column ───────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'EmployeeId')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Timesheets_Employees_EmployeeId')
                    ALTER TABLE [Timesheets] DROP CONSTRAINT [FK_Timesheets_Employees_EmployeeId];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Timesheets_EmployeeId_WeekStartDate' AND object_id = OBJECT_ID(N'[Timesheets]'))
                    DROP INDEX [IX_Timesheets_EmployeeId_WeekStartDate] ON [Timesheets];
                ALTER TABLE [Timesheets] DROP COLUMN [EmployeeId];
            END
            """);

        // ── 7e. Timesheets – add FK to Resources and new unique index ────────
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Timesheets_Resources_UserId')
               AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'UserId')
            BEGIN
                ALTER TABLE [Timesheets] ADD CONSTRAINT [FK_Timesheets_Resources_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [Resources] ([UserId]) ON DELETE CASCADE;
                CREATE UNIQUE INDEX [IX_Timesheets_UserId_WeekStartDate] ON [Timesheets] ([UserId], [WeekStartDate]);
            END
            """);

        // ── 8. Create ResourceSkills from EmployeeSkills ─────────────────────
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[EmployeeSkills]', N'U') IS NOT NULL AND OBJECT_ID(N'[ResourceSkills]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ResourceSkills] (
                    [Id]              int          NOT NULL IDENTITY,
                    [UserId]          int          NOT NULL,
                    [SkillId]         int          NOT NULL,
                    [Category]        nvarchar(20) NOT NULL,
                    [ProficiencyLevel] nvarchar(20) NOT NULL,
                    CONSTRAINT [PK_ResourceSkills] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ResourceSkills_Resources_UserId] FOREIGN KEY ([UserId])   REFERENCES [Resources] ([UserId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_ResourceSkills_Skills_SkillId]   FOREIGN KEY ([SkillId])  REFERENCES [Skills]    ([Id])     ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_ResourceSkills_UserId_SkillId] ON [ResourceSkills] ([UserId], [SkillId]);

                INSERT INTO [ResourceSkills] ([UserId], [SkillId], [Category], [ProficiencyLevel])
                SELECT e.[UserId], es.[SkillId], es.[Category], es.[ProficiencyLevel]
                FROM   [EmployeeSkills] es
                INNER JOIN [Employees] e ON e.[Id] = es.[EmployeeId];

                DROP TABLE [EmployeeSkills];
            END
            """);

        // ── 9. Drop Employees table ──────────────────────────────────────────
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[Employees]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Users_UserId')
                    ALTER TABLE [Employees] DROP CONSTRAINT [FK_Employees_Users_UserId];
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Users_ManagerId')
                    ALTER TABLE [Employees] DROP CONSTRAINT [FK_Employees_Users_ManagerId];
                DROP TABLE [Employees];
            END
            """);

        // ── 10. Drop legacy Users.Role column ───────────────────────────────
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Role')
            BEGIN
                ALTER TABLE [Users] DROP COLUMN [Role];
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Down migration is not supported for RefactorEmployeeToResource.");
    }
}
