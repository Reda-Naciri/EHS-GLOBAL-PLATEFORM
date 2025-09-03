using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class UserTrackingSystemFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LocalJobTitle",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LaborIndicator",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountCreatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountUpdatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatus",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-default-id",
                columns: new[] { "AccountCreatedAt", "AccountUpdatedAt", "CompanyId", "CurrentStatus", "DateOfBirth", "DepartmentId", "IsOnline", "LastActivityAt", "LastLoginAt", "Position", "ShiftId", "ZoneId" },
                values: new object[] { new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "ADMIN001", null, new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, false, null, null, "System Administrator", 4, 8 });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "hse-default-id",
                columns: new[] { "AccountCreatedAt", "AccountUpdatedAt", "CompanyId", "CurrentStatus", "DateOfBirth", "DepartmentId", "IsOnline", "LastActivityAt", "LastLoginAt", "Position", "ShiftId", "ZoneId" },
                values: new object[] { new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HSE001", null, new DateTime(1985, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, false, null, null, "HSE Manager", 4, 1 });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-1",
                columns: new[] { "AccountCreatedAt", "AccountUpdatedAt", "CompanyId", "CurrentStatus", "DateOfBirth", "DepartmentId", "IsOnline", "LastActivityAt", "LastLoginAt", "PasswordHash", "Position", "ShiftId", "ZoneId" },
                values: new object[] { new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TE001234", null, new DateTime(1992, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, false, null, null, null, "Production Operator", 1, 1 });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-2",
                columns: new[] { "AccountCreatedAt", "AccountUpdatedAt", "CompanyId", "CurrentStatus", "DateOfBirth", "DepartmentId", "IsOnline", "LastActivityAt", "LastLoginAt", "PasswordHash", "Position", "ShiftId", "ZoneId" },
                values: new object[] { new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TE005678", null, new DateTime(1988, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, false, null, null, null, "Quality Inspector", 2, 4 });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "IT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Information Technology Department", true, "IT Administration", null },
                    { 2, "HSE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Health, Safety and Environment Department", true, "Health Safety Environment", null },
                    { 3, "PROD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Production Department", true, "Production", null },
                    { 4, "QUA", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quality Control Department", true, "Quality", null },
                    { 5, "LOG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Logistics Department", true, "Logistics", null },
                    { 6, "ENG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering Department", true, "Engineering", null },
                    { 7, "OPS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Operations Department", true, "Operations", null },
                    { 8, "MAINT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Maintenance Department", true, "Maintenance", null }
                });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "EndTime", "IsActive", "Name", "StartTime", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "DAY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "6:00 AM - 2:00 PM", new TimeSpan(0, 14, 0, 0, 0), true, "Day Shift", new TimeSpan(0, 6, 0, 0, 0), null },
                    { 2, "AFT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2:00 PM - 10:00 PM", new TimeSpan(0, 22, 0, 0, 0), true, "Afternoon Shift", new TimeSpan(0, 14, 0, 0, 0), null },
                    { 3, "NIGHT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "10:00 PM - 6:00 AM", new TimeSpan(0, 6, 0, 0, 0), true, "Night Shift", new TimeSpan(0, 22, 0, 0, 0), null },
                    { 4, "OFFICE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "8:00 AM - 5:00 PM", new TimeSpan(0, 17, 0, 0, 0), true, "Office Hours", new TimeSpan(0, 8, 0, 0, 0), null }
                });

            migrationBuilder.InsertData(
                table: "Zones",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "PROD-A", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Main production area", true, "Production Area A", null },
                    { 2, "PROD-B", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Secondary production area", true, "Production Area B", null },
                    { 3, "WH-A", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Main warehouse", true, "Warehouse A", null },
                    { 4, "WH-B", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Secondary warehouse", true, "Warehouse B", null },
                    { 5, "OFFICE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrative offices", true, "Office Building", null },
                    { 6, "LAB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quality testing laboratory", true, "Laboratory", null },
                    { 7, "DOCK", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Loading and unloading area", true, "Loading Dock", null },
                    { 8, "ALL", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Access to all areas", true, "All Areas", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ShiftId",
                table: "Reports",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ZoneId",
                table: "Reports",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ShiftId",
                table: "AspNetUsers",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ZoneId",
                table: "AspNetUsers",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_UserId",
                table: "UserActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Shifts_ShiftId",
                table: "AspNetUsers",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Zones_ZoneId",
                table: "AspNetUsers",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Shifts_ShiftId",
                table: "Reports",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Zones_ZoneId",
                table: "Reports",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Shifts_ShiftId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Zones_ZoneId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Shifts_ShiftId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Zones_ZoneId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "UserActivities");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ShiftId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ZoneId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ShiftId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ZoneId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AccountCreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AccountUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentStatus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LocalJobTitle",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LaborIndicator",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-default-id",
                columns: new[] { "DateOfBirth", "Position" },
                values: new object[] { null, "" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "hse-default-id",
                columns: new[] { "DateOfBirth", "Position" },
                values: new object[] { null, "" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-1",
                columns: new[] { "DateOfBirth", "PasswordHash", "Position" },
                values: new object[] { null, "AQAAAAIAAYagAAAAECCFEZqGRq8/9qTFZpMEBwGkNpRHOYqOyqUiJjgRhiJRPpUbqLJSJJJgQl5wSPbzBw==", "" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-2",
                columns: new[] { "DateOfBirth", "PasswordHash", "Position" },
                values: new object[] { null, "AQAAAAIAAYagAAAAECCFEZqGRq8/9qTFZpMEBwGkNpRHOYqOyqUiJjgRhiJRPpUbqLJSJJJgQl5wSPbzBw==", "" });
        }
    }
}
