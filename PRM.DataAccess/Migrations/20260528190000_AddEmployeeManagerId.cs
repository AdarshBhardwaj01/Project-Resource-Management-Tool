using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PRM.DataAccess.Context;
#nullable disable

namespace PRM.DataAccess.Migrations;
[DbContext(typeof(PrmDbContext))]
[Migration("20260528190000_AddEmployeeManagerId")]

public partial class AddEmployeeManagerId : Migration
{

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ManagerId",
            table: "Employees",
            type: "int",
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Employees_ManagerId",
            table: "Employees",
            column: "ManagerId");
        migrationBuilder.AddForeignKey(
            name: "FK_Employees_Users_ManagerId",
            table: "Employees",
            column: "ManagerId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Employees_Users_ManagerId",
            table: "Employees");
        migrationBuilder.DropIndex(
            name: "IX_Employees_ManagerId",
            table: "Employees");
        migrationBuilder.DropColumn(
            name: "ManagerId",
            table: "Employees");
    }

}
