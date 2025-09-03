using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHourColumnsUnifyMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, migrate existing data from hours to minutes (if minute columns are null)
            migrationBuilder.Sql(@"
                UPDATE EmailConfigurations 
                SET HSEUpdateIntervalMinutes = CAST(HSEUpdateIntervalHours * 60 AS INT)
                WHERE HSEUpdateIntervalMinutes IS NULL;
            ");
            
            migrationBuilder.Sql(@"
                UPDATE EmailConfigurations 
                SET AdminOverviewIntervalMinutes = CAST(AdminOverviewIntervalHours * 60 AS INT)
                WHERE AdminOverviewIntervalMinutes IS NULL;
            ");
            
            // Make minute columns non-nullable with proper default (360 minutes = 6 hours)
            migrationBuilder.AlterColumn<int>(
                name: "HSEUpdateIntervalMinutes",
                table: "EmailConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 360,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AdminOverviewIntervalMinutes",
                table: "EmailConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 360,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Now drop the old hour columns
            migrationBuilder.DropColumn(
                name: "AdminOverviewIntervalHours",
                table: "EmailConfigurations");

            migrationBuilder.DropColumn(
                name: "HSEUpdateIntervalHours",
                table: "EmailConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "HSEUpdateIntervalMinutes",
                table: "EmailConfigurations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AdminOverviewIntervalMinutes",
                table: "EmailConfigurations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "AdminOverviewIntervalHours",
                table: "EmailConfigurations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "HSEUpdateIntervalHours",
                table: "EmailConfigurations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
