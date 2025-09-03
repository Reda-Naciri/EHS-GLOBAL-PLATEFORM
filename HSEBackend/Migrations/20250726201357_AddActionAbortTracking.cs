using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddActionAbortTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbortReason",
                table: "Actions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AbortedAt",
                table: "Actions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbortedById",
                table: "Actions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AbortedById",
                table: "Actions",
                column: "AbortedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_AspNetUsers_AbortedById",
                table: "Actions",
                column: "AbortedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_AspNetUsers_AbortedById",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_AbortedById",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "AbortReason",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "AbortedAt",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "AbortedById",
                table: "Actions");
        }
    }
}
