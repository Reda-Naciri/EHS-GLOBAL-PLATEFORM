using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixNullableReportIdAndOverdue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Overdue columns only if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SubActions') AND name = 'Overdue')
                BEGIN
                    ALTER TABLE SubActions ADD Overdue bit NOT NULL DEFAULT 0
                END
            ");

            // Make ReportId nullable in CorrectiveActions table
            migrationBuilder.AlterColumn<int>(
                name: "ReportId",
                table: "CorrectiveActions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'CorrectiveActions') AND name = 'Overdue')
                BEGIN
                    ALTER TABLE CorrectiveActions ADD Overdue bit NOT NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Actions') AND name = 'Overdue')
                BEGIN
                    ALTER TABLE Actions ADD Overdue bit NOT NULL DEFAULT 0
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Overdue",
                table: "SubActions");

            migrationBuilder.DropColumn(
                name: "Overdue",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "Overdue",
                table: "Actions");

            migrationBuilder.AlterColumn<int>(
                name: "ReportId",
                table: "CorrectiveActions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
