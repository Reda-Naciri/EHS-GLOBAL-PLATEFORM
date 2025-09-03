using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIntervalHoursToDouble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "HSEUpdateIntervalHours",
                table: "EmailConfigurations",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "AdminOverviewIntervalHours",
                table: "EmailConfigurations",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "HSEUpdateIntervalHours",
                table: "EmailConfigurations",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "AdminOverviewIntervalHours",
                table: "EmailConfigurations",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
