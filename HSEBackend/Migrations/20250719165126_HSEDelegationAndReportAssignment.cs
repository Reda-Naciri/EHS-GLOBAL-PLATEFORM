using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class HSEDelegationAndReportAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HSEZoneDelegations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromHSEUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ToHSEUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HSEZoneDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HSEZoneDelegations_AspNetUsers_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HSEZoneDelegations_AspNetUsers_FromHSEUserId",
                        column: x => x.FromHSEUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HSEZoneDelegations_AspNetUsers_ToHSEUserId",
                        column: x => x.ToHSEUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HSEZoneDelegations_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    AssignedHSEUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportAssignments_AspNetUsers_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportAssignments_AspNetUsers_AssignedHSEUserId",
                        column: x => x.AssignedHSEUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportAssignments_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HSEZoneDelegations_CreatedByAdminId",
                table: "HSEZoneDelegations",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_HSEZoneDelegations_FromHSEUserId",
                table: "HSEZoneDelegations",
                column: "FromHSEUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HSEZoneDelegations_ToHSEUserId",
                table: "HSEZoneDelegations",
                column: "ToHSEUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HSEZoneDelegations_ZoneId",
                table: "HSEZoneDelegations",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAssignments_AssignedByAdminId",
                table: "ReportAssignments",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAssignments_AssignedHSEUserId",
                table: "ReportAssignments",
                column: "AssignedHSEUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAssignments_ReportId",
                table: "ReportAssignments",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HSEZoneDelegations");

            migrationBuilder.DropTable(
                name: "ReportAssignments");
        }
    }
}
