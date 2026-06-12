using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PRM.DataAccess.Context;

#nullable disable

namespace PRM.DataAccess.Migrations;

[DbContext(typeof(PrmDbContext))]
[Migration("20260612140000_AddTimesheetWorkflowAndAtRiskNotification")]
public partial class AddTimesheetWorkflowAndAtRiskNotification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'IsFrozen')
            BEGIN
                ALTER TABLE [Timesheets] ADD [IsFrozen] bit NOT NULL CONSTRAINT [DF_Timesheets_IsFrozen] DEFAULT 0;
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'IsUnlockedByManager')
            BEGIN
                ALTER TABLE [Timesheets] ADD [IsUnlockedByManager] bit NOT NULL CONSTRAINT [DF_Timesheets_IsUnlockedByManager] DEFAULT 0;
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'ReminderCount')
            BEGIN
                ALTER TABLE [Timesheets] ADD [ReminderCount] int NOT NULL CONSTRAINT [DF_Timesheets_ReminderCount] DEFAULT 0;
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'AtRiskNotificationSentAt')
            BEGIN
                ALTER TABLE [Projects] ADD [AtRiskNotificationSentAt] datetime2 NULL;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'IsFrozen')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Timesheets_IsFrozen')
                    ALTER TABLE [Timesheets] DROP CONSTRAINT [DF_Timesheets_IsFrozen];
                ALTER TABLE [Timesheets] DROP COLUMN [IsFrozen];
            END
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'IsUnlockedByManager')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Timesheets_IsUnlockedByManager')
                    ALTER TABLE [Timesheets] DROP CONSTRAINT [DF_Timesheets_IsUnlockedByManager];
                ALTER TABLE [Timesheets] DROP COLUMN [IsUnlockedByManager];
            END
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Timesheets]') AND name = 'ReminderCount')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Timesheets_ReminderCount')
                    ALTER TABLE [Timesheets] DROP CONSTRAINT [DF_Timesheets_ReminderCount];
                ALTER TABLE [Timesheets] DROP COLUMN [ReminderCount];
            END
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'AtRiskNotificationSentAt')
            BEGIN
                ALTER TABLE [Projects] DROP COLUMN [AtRiskNotificationSentAt];
            END
            """);
    }
}
