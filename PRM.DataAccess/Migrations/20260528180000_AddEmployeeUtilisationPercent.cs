using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PRM.DataAccess.Context;
#nullable disable

namespace PRM.DataAccess.Migrations
{
    [DbContext(typeof(PrmDbContext))]
    [Migration("20260528180000_AddEmployeeUtilisationPercent")]
    public partial class AddEmployeeUtilisationPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UtilisationPercent",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UtilisationPercent",
                table: "Employees");
        }
    }
}
