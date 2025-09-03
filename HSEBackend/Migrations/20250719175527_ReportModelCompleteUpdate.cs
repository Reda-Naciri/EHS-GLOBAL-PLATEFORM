using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class ReportModelCompleteUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyPart",
                table: "Injuries");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CorrectiveActions");

            migrationBuilder.RenameColumn(
                name: "ReporterId",
                table: "Reports",
                newName: "ReporterCompanyId");

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "Reports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenedByHSEId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BodyPartId",
                table: "Injuries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FractureTypeId",
                table: "Injuries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToProfileId",
                table: "CorrectiveActions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByHSEId",
                table: "CorrectiveActions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BodyParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FractureTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FractureTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TriggeredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RelatedReportId = table.Column<int>(type: "int", nullable: true),
                    RelatedActionId = table.Column<int>(type: "int", nullable: true),
                    RelatedCorrectiveActionId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Actions_RelatedActionId",
                        column: x => x.RelatedActionId,
                        principalTable: "Actions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_CorrectiveActions_RelatedCorrectiveActionId",
                        column: x => x.RelatedCorrectiveActionId,
                        principalTable: "CorrectiveActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Reports_RelatedReportId",
                        column: x => x.RelatedReportId,
                        principalTable: "Reports",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "BodyParts",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "HEAD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Head and skull area", true, "Head", null },
                    { 2, "EYES", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Eye area", true, "Eyes", null },
                    { 3, "FACE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Facial area", true, "Face", null },
                    { 4, "NECK", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Neck area", true, "Neck", null },
                    { 5, "L_SHOULDER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Left shoulder", true, "Left Shoulder", null },
                    { 6, "R_SHOULDER", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Right shoulder", true, "Right Shoulder", null },
                    { 7, "L_ARM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Left arm", true, "Left Arm", null },
                    { 8, "R_ARM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Right arm", true, "Right Arm", null },
                    { 9, "L_HAND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Left hand and fingers", true, "Left Hand", null },
                    { 10, "R_HAND", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Right hand and fingers", true, "Right Hand", null },
                    { 11, "CHEST", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chest area", true, "Chest", null },
                    { 12, "BACK", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Back area", true, "Back", null },
                    { 13, "ABDOMEN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Abdominal area", true, "Abdomen", null },
                    { 14, "L_LEG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Left leg", true, "Left Leg", null },
                    { 15, "R_LEG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Right leg", true, "Right Leg", null },
                    { 16, "L_FOOT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Left foot and toes", true, "Left Foot", null },
                    { 17, "R_FOOT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Right foot and toes", true, "Right Foot", null }
                });

            migrationBuilder.InsertData(
                table: "FractureTypes",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Cut", "CUT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cuts and lacerations", true, "Cut/Laceration", null },
                    { 2, "Bruise", "BRUISE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bruises and contusions", true, "Bruise/Contusion", null },
                    { 3, "Burn", "BURN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thermal burns", true, "Burn", null },
                    { 4, "Burn", "CHEM_BURN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chemical burns", true, "Chemical Burn", null },
                    { 5, "Fracture", "SIMPLE_FRAC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Simple bone fracture", true, "Simple Fracture", null },
                    { 6, "Fracture", "COMPOUND_FRAC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Compound bone fracture", true, "Compound Fracture", null },
                    { 7, "Sprain", "SPRAIN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Joint sprain", true, "Sprain", null },
                    { 8, "Strain", "STRAIN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Muscle strain", true, "Strain", null },
                    { 9, "Cut", "PUNCTURE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Puncture wounds", true, "Puncture Wound", null },
                    { 10, "Cut", "ABRASION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Abrasions and scrapes", true, "Abrasion/Scrape", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_OpenedByHSEId",
                table: "Reports",
                column: "OpenedByHSEId");

            migrationBuilder.CreateIndex(
                name: "IX_Injuries_BodyPartId",
                table: "Injuries",
                column: "BodyPartId");

            migrationBuilder.CreateIndex(
                name: "IX_Injuries_FractureTypeId",
                table: "Injuries",
                column: "FractureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_AssignedToProfileId",
                table: "CorrectiveActions",
                column: "AssignedToProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_CreatedByHSEId",
                table: "CorrectiveActions",
                column: "CreatedByHSEId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedActionId",
                table: "Notifications",
                column: "RelatedActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedCorrectiveActionId",
                table: "Notifications",
                column: "RelatedCorrectiveActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedReportId",
                table: "Notifications",
                column: "RelatedReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TriggeredByUserId",
                table: "Notifications",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CorrectiveActions_AspNetUsers_AssignedToProfileId",
                table: "CorrectiveActions",
                column: "AssignedToProfileId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CorrectiveActions_AspNetUsers_CreatedByHSEId",
                table: "CorrectiveActions",
                column: "CreatedByHSEId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Injuries_BodyParts_BodyPartId",
                table: "Injuries",
                column: "BodyPartId",
                principalTable: "BodyParts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Injuries_FractureTypes_FractureTypeId",
                table: "Injuries",
                column: "FractureTypeId",
                principalTable: "FractureTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_OpenedByHSEId",
                table: "Reports",
                column: "OpenedByHSEId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CorrectiveActions_AspNetUsers_AssignedToProfileId",
                table: "CorrectiveActions");

            migrationBuilder.DropForeignKey(
                name: "FK_CorrectiveActions_AspNetUsers_CreatedByHSEId",
                table: "CorrectiveActions");

            migrationBuilder.DropForeignKey(
                name: "FK_Injuries_BodyParts_BodyPartId",
                table: "Injuries");

            migrationBuilder.DropForeignKey(
                name: "FK_Injuries_FractureTypes_FractureTypeId",
                table: "Injuries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_OpenedByHSEId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "BodyParts");

            migrationBuilder.DropTable(
                name: "FractureTypes");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Reports_OpenedByHSEId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Injuries_BodyPartId",
                table: "Injuries");

            migrationBuilder.DropIndex(
                name: "IX_Injuries_FractureTypeId",
                table: "Injuries");

            migrationBuilder.DropIndex(
                name: "IX_CorrectiveActions_AssignedToProfileId",
                table: "CorrectiveActions");

            migrationBuilder.DropIndex(
                name: "IX_CorrectiveActions_CreatedByHSEId",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "OpenedByHSEId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "BodyPartId",
                table: "Injuries");

            migrationBuilder.DropColumn(
                name: "FractureTypeId",
                table: "Injuries");

            migrationBuilder.DropColumn(
                name: "AssignedToProfileId",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "CreatedByHSEId",
                table: "CorrectiveActions");

            migrationBuilder.RenameColumn(
                name: "ReporterCompanyId",
                table: "Reports",
                newName: "ReporterId");

            migrationBuilder.AddColumn<string>(
                name: "BodyPart",
                table: "Injuries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "CorrectiveActions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CorrectiveActions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
