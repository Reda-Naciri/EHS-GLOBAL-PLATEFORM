using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class UserActiveStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-default-id",
                columns: new[] { "DeactivatedAt", "DeactivationReason", "IsActive" },
                values: new object[] { null, null, true });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "hse-default-id",
                columns: new[] { "DeactivatedAt", "DeactivationReason", "IsActive" },
                values: new object[] { null, null, true });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-1",
                columns: new[] { "DeactivatedAt", "DeactivationReason", "IsActive" },
                values: new object[] { null, null, true });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-2",
                columns: new[] { "DeactivatedAt", "DeactivationReason", "IsActive" },
                values: new object[] { null, null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");
        }
    }
}
