using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class HSEZoneResponsibilitySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Zones_ZoneId",
                table: "Reports");

            migrationBuilder.CreateTable(
                name: "HSEZoneResponsibilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HSEUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HSEZoneResponsibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HSEZoneResponsibilities_AspNetUsers_HSEUserId",
                        column: x => x.HSEUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HSEZoneResponsibilities_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HSEZoneResponsibilities_HSEUserId",
                table: "HSEZoneResponsibilities",
                column: "HSEUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HSEZoneResponsibilities_ZoneId",
                table: "HSEZoneResponsibilities",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Zones_ZoneId",
                table: "Reports",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Zones_ZoneId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "HSEZoneResponsibilities");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Zones_ZoneId",
                table: "Reports",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id");
        }
    }
}
