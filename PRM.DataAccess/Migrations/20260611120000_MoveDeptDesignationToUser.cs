using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PRM.DataAccess.Context;

#nullable disable

namespace PRM.DataAccess.Migrations;

[DbContext(typeof(PrmDbContext))]
[Migration("20260611120000_MoveDeptDesignationToUser")]
public partial class MoveDeptDesignationToUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add Department and Designation columns to Users table
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Department')
            BEGIN
                ALTER TABLE [Users] ADD [Department] nvarchar(100) NOT NULL CONSTRAINT [DF_Users_Department] DEFAULT N'';
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Designation')
            BEGIN
                ALTER TABLE [Users] ADD [Designation] nvarchar(100) NOT NULL CONSTRAINT [DF_Users_Designation] DEFAULT N'';
            END
            """);

        // Copy existing Department and Designation values from Resources to Users
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Resources]') AND name = 'Department')
               AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Department')
            BEGIN
                UPDATE u
                SET u.[Department] = r.[Department],
                    u.[Designation] = r.[Designation]
                FROM [Users] u
                INNER JOIN [Resources] r ON r.[UserId] = u.[Id];
            END
            """);

        // Drop Department and Designation columns from Resources table
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Resources]') AND name = 'Department')
            BEGIN
                ALTER TABLE [Resources] DROP COLUMN [Department];
            END
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Resources]') AND name = 'Designation')
            BEGIN
                ALTER TABLE [Resources] DROP COLUMN [Designation];
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore Department and Designation columns on Resources
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Resources]') AND name = 'Department')
            BEGIN
                ALTER TABLE [Resources] ADD [Department] nvarchar(50) NOT NULL CONSTRAINT [DF_Resources_Department] DEFAULT N'';
                ALTER TABLE [Resources] ADD [Designation] nvarchar(50) NOT NULL CONSTRAINT [DF_Resources_Designation] DEFAULT N'';
                UPDATE r
                SET r.[Department] = u.[Department],
                    r.[Designation] = u.[Designation]
                FROM [Resources] r
                INNER JOIN [Users] u ON u.[Id] = r.[UserId];
            END
            """);

        // Drop Department and Designation from Users
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Department')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_Department')
                    ALTER TABLE [Users] DROP CONSTRAINT [DF_Users_Department];
                ALTER TABLE [Users] DROP COLUMN [Department];
            END
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'Designation')
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_Designation')
                    ALTER TABLE [Users] DROP CONSTRAINT [DF_Users_Designation];
                ALTER TABLE [Users] DROP COLUMN [Designation];
            END
            """);
    }
}
