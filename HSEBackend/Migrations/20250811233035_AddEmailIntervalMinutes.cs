using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailIntervalMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminOverviewIntervalMinutes",
                table: "EmailConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HSEUpdateIntervalMinutes",
                table: "EmailConfigurations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminOverviewIntervalMinutes",
                table: "EmailConfigurations");

            migrationBuilder.DropColumn(
                name: "HSEUpdateIntervalMinutes",
                table: "EmailConfigurations");
        }
    }
}
